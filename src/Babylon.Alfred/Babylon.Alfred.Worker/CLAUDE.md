# Babylon.Alfred.Worker - Background Worker Service

## Overview

A .NET hosted service that runs scheduled background jobs via Quartz.NET. Responsible for fetching market prices, creating portfolio snapshots, and running data backfills. References `Babylon.Alfred.Api` for shared models, repositories, and calculators.

## Project Structure

```
Babylon.Alfred.Worker/
├── Program.cs                           # Host builder, DI, Quartz configuration
├── appsettings.json                     # Connection string, logging, schedule config
├── Dockerfile                           # Multi-stage Docker build
├── Extensions/
│   └── ServiceCollectionExtensions.cs   # HttpClient registration for Yahoo Finance
├── Jobs/
│   ├── IJob.cs                          # IJobBase : Quartz.IJob (base interface)
│   ├── PriceFetchingJob.cs              # Hourly price updates from Yahoo Finance
│   └── PortfolioSnapshotJob.cs          # Hourly portfolio value snapshots
└── Services/
    ├── PriceFetchingService.cs          # Orchestrates price fetching with rate limiting
    ├── YahooFinanceService.cs           # HTTP client for Yahoo Finance v8 API
    ├── PortfolioSnapshotService.cs      # Creates portfolio value snapshots
    └── RealizedGainsBackfillService.cs  # One-time startup backfill of realized P&L
```

## Scheduled Jobs

### PriceFetchingJob
- **Schedule**: `0 0 9-22 ? * MON-FRI` (top of every hour, 9AM-10PM UTC, weekdays)
- **Concurrency**: `[DisallowConcurrentExecution]`
- **Behavior**:
  1. Queries securities that haven't been updated in the last hour.
  2. Limits to 50 API calls per run (rate limiting).
  3. 3-second delay between calls, escalates +5s after rate limit responses.
  4. Retries up to 3 times with exponential backoff (10s, 20s, 40s).
  5. Calls Yahoo Finance v8 API for each ticker.
  6. Upserts price in `market_prices` table.
  7. Marks invalid tickers to prevent future retries.

### PortfolioSnapshotJob
- **Schedule**: `0 15 9-22 ? * MON-FRI` (15 minutes past each hour, weekdays)
- **Concurrency**: `[DisallowConcurrentExecution]`
- **Behavior**:
  1. Queries all users with portfolios (distinct user IDs from transactions).
  2. For each user: fetches cash balance, all transactions, and market prices.
  3. Calculates FIFO cost basis per position.
  4. Creates `PortfolioSnapshot` record with TotalInvested, CashBalance, TotalMarketValue, UnrealizedPnL.
  5. Skips users with no holdings and no cash.

## Background Services

### RealizedGainsBackfillService
- **Type**: `BackgroundService` (runs once on startup, then completes)
- **Purpose**: One-time backfill of `RealizedPnL` and `RealizedPnLPct` fields on Transaction records.
- **Behavior**:
  1. Queries all unique user IDs from transactions.
  2. Groups transactions by security.
  3. Uses `RealizedPnLCalculator.CalculateRealizedPnLByTransactionId()`.
  4. Updates transaction records only if values changed.
  5. Logs update counts per user.

## Shared Code (from Babylon.Alfred.Api)

The Worker project references the API project and reuses:
- **Domain models**: Transaction, Security, MarketPrice, PortfolioSnapshot, CashBalance, User.
- **Repositories**: AllocationStrategy, MarketPrice, PortfolioSnapshot, Transaction, Security, CashBalance.
- **Calculators**: `PortfolioCalculator` (FIFO cost basis), `RealizedPnLCalculator`.
- **DbContext**: `BabylonDbContext`.

## Yahoo Finance API

- **Endpoint**: `https://query2.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range=1d`
- **User-Agent**: Chrome 124.0 (mimics browser to avoid blocks).
- **Response parsing**: Extracts `regularMarketPrice` or falls back to `previousClose`.
- **Error handling**: 404 (ticker not found, marks as invalid), 429 (rate limited, exponential backoff).
- **Returns**: `YahooPriceResult` with Price and Currency.

## Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=babylon_dev;SslMode=Require"
  },
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "/logs/babylon-worker-.log", "rollingInterval": "Day", "retainedFileCountLimit": 7 } }
    ]
  }
}
```

## Resilience Patterns

- **Database**: Npgsql retry on transient failures (3 retries, 5s delay).
- **Yahoo API**: Exponential backoff on 429s. Max 3 retries per ticker. Stops job early after 3 consecutive rate limits.
- **Jobs**: `DisallowConcurrentExecution` prevents overlapping. CancellationToken for graceful shutdown.
- **Scoping**: Each Quartz job execution gets a new DI scope for clean repository state.

## Docker

Multi-stage build identical to API. Runtime: `mcr.microsoft.com/dotnet/aspnet:9.0`. Entry: `Babylon.Alfred.Worker.dll`.
