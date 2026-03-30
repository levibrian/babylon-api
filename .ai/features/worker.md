# Worker Service — Background Jobs

## Overview

`Babylon.Alfred.Worker` is a .NET hosted service running scheduled background jobs via Quartz.NET. References `Babylon.Alfred.Api` project for shared models, repositories, and calculators.

---

## Scheduled Jobs

### PriceFetchingJob
- **Schedule**: `0 0 9-22 ? * MON-FRI` (top of every hour, 9AM–10PM UTC, weekdays)
- **Concurrency**: `[DisallowConcurrentExecution]`
- **Behavior**:
  1. Queries securities not updated in the last hour
  2. Limits to 50 API calls per run (rate limiting)
  3. 3-second delay between calls; escalates +5s after rate-limit responses
  4. Retries up to 3 times with exponential backoff (10s, 20s, 40s)
  5. Calls Yahoo Finance v8 API per ticker
  6. Upserts price in `market_prices` table
  7. Marks invalid tickers to prevent future retries

### PortfolioSnapshotJob
- **Schedule**: `0 15 9-22 ? * MON-FRI` (15 minutes past each hour, weekdays)
- **Rationale**: 15-minute offset ensures PriceFetchingJob completes first
- **Concurrency**: `[DisallowConcurrentExecution]`
- **Behavior**:
  1. Queries all users with portfolios
  2. For each user: fetches cash balance, all transactions, market prices
  3. Calculates FIFO cost basis per position
  4. Creates `PortfolioSnapshot` with TotalInvested, CashBalance, TotalMarketValue, UnrealizedPnL
  5. Skips users with no holdings and no cash

### RealizedPnlBackfillJob
- **Schedule**: `0 0 3 * * ?` (3:00 AM UTC, daily)
- **Concurrency**: `[DisallowConcurrentExecution]`
- **Idempotency**: Only queries users with at least one Sell where `RealizedPnL IS NULL`. No-op once all rows are populated.
- **Per-user resilience**: Exceptions for one user are caught and logged; other users continue.
- **Behavior**:
  1. Queries distinct users with unbackfilled Sell transactions
  2. For each user, fetches ALL transactions grouped by security (FIFO reconstruction)
  3. Uses `RealizedPnLCalculator.CalculateRealizedPnLByTransactionId()`
  4. Skips Sells already populated or with no buy lots
  5. Batch-updates changed records via `UpdateBulkAsync`

---

## Shared Code (from Babylon.Alfred.Api)

The Worker reuses:
- **Domain models**: Transaction, Security, MarketPrice, PortfolioSnapshot, CashBalance, User
- **Repositories**: AllocationStrategy, MarketPrice, PortfolioSnapshot, Transaction, Security, CashBalance
- **Calculators**: `PortfolioCalculator` (FIFO), `RealizedPnLCalculator`
- **DbContext**: `BabylonDbContext`

---

## Yahoo Finance API

- **Endpoint**: `https://query2.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range=1d`
- **User-Agent**: Chrome 124.0 (mimics browser to avoid blocks)
- **Response**: Extracts `regularMarketPrice` or falls back to `previousClose`
- **Error handling**: 404 → mark as invalid, 429 → exponential backoff

---

## Resilience Patterns

- **Database**: Npgsql retry on transient failures (3 retries, 5s delay)
- **Yahoo API**: Exponential backoff on 429s. Max 3 retries per ticker. Stops job early after 3 consecutive rate limits.
- **Jobs**: `DisallowConcurrentExecution` prevents overlapping. `CancellationToken` for graceful shutdown.
- **Scoping**: Each Quartz job execution gets a new DI scope for clean repository state.

---

## Test Strategy

Jobs are **not** directly unit tested. Services (`PriceFetchingService`, `PortfolioSnapshotService`) are tested in isolation via mocked repositories.

---

## Docker

Multi-stage build. Runtime: `mcr.microsoft.com/dotnet/aspnet:9.0`. Entry: `Babylon.Alfred.Worker.dll`.
