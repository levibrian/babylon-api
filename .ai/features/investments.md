# Investments Feature

## Overview

Core feature. Tracks transactions across asset types, computes FIFO positions, realized/unrealized P&L, cash and portfolio history.

---

## Component Inventory

| Layer | Components |
|-------|-----------|
| Controllers (6) | Portfolios, PortfolioHistory, Transactions, Securities, Cash, Allocations |
| Services (7) | Portfolio, PortfolioHistory, Transaction, Security, MarketPrice, CashBalance, Allocation |
| Calculators | `PortfolioCalculator` (FIFO, allocation, rebalancing math), `RealizedPnLCalculator`, `DividendCalculator` |
| Validators | `TransactionValidator`, `SecurityValidator` |
| Helpers | `TransactionMapper`, `TransactionOrdering`, `ErrorMessages` |

---

## Endpoints

| Route | Methods | Auth |
|-------|---------|------|
| `/api/v1/portfolios` | GET (positions + cash) | ✅ |
| `/api/v1/portfolios/history` | GET, GET `latest` | ✅ |
| `/api/v1/transactions` | POST, POST `bulk`, GET, PUT `{id}`, DELETE `{id}` | ✅ |
| `/api/v1/cash` | PUT | ✅ |
| `/api/v1/allocations` | GET (user's targets), PUT (replace full target set) | ✅ |
| `/api/v1/securities` | GET, GET `{ticker}`, POST (Yahoo lookup by ticker), POST `admin` (full metadata, no Yahoo), PUT `{ticker}`, DELETE `{ticker}` | ❌ none |

---

## Business Rules

### FIFO Cost Basis Algorithm

1. Maintain a queue of "lots" (buy transactions, ordered by date)
2. **Buy**: Add new lot. `TotalCost = (Shares × Price) + Fees`. **Tax NOT included.**
3. **Sell**: Consume lots from front of queue. `NetProceeds = (Shares × Price) - Fees`. **Tax NOT deducted.** Realized P&L = `NetProceeds - ConsumedCostBasis`
4. **Split**: Multiply shares in ALL existing lots by split ratio. Price = 0. No money changes hands.
5. **Dividend**: Does NOT affect cost basis. Net income = `GrossAmount - Tax`. **Tax IS applied here only.**

### Transaction.TotalAmount (computed, not persisted)

| Type | Formula | Tax used? |
|------|---------|-----------|
| Buy | `(Shares × Price) + Fees` | **NO** |
| Sell | `(Shares × Price) - Fees` | **NO** |
| Dividend | `(Shares × Price) - Tax` | **YES** |
| Split | `0` | NO |

### DO NOT Rules

- **DO NOT** include `Tax` in Buy lot `TotalCost`.
- **DO NOT** deduct `Tax` from Sell `NetProceeds`.
- **DO NOT** apply `Tax` to Buy or Sell `TotalAmount`. Tax is exclusively a Dividend concern.
- **DO NOT** add a new transaction type without updating: FIFO algorithm, `TotalAmount` switch, Tax applicability table, and this file.
- **DO NOT** add `Fees` to Dividend cost basis or treat Dividend as a purchase.
- **DO NOT** recalculate `RealizedPnL` in the service layer — use `RealizedPnLCalculator` or `PortfolioCalculator`.

### Transaction Rules

- On **creation**: `UpdatedAt` is set to the transaction `Date`
- On **update**: `UpdatedAt` is set to `DateTime.UtcNow`
- **Sell validation**: cannot sell more shares than currently held (FIFO-aware)
- Bulk insert supported for batch imports

### Portfolio Rules

- Fully-sold positions (net shares = 0) are excluded from open positions
- Positions are ordered by current market value DESC (falls back to total invested)
- Market value = `TotalShares × CurrentPrice`, prices from `market_prices` (updated hourly by the Worker)
- Cash balance is tracked separately and included in the portfolio response

### Allocation targets

- `AllocationStrategy.TargetPercentage` is the single source of truth: an explicit % of the **total portfolio** (positions + cash) per security
- `GET /api/v1/allocations` → `[{ ticker, targetPercentage }]`, ordered by target DESC
- `PUT /api/v1/allocations` with `{ targets: [{ ticker, targetPercentage }] }` replaces the user's full set (missing tickers are removed; empty list clears all)
- Validation (400): each target 0–100 inclusive, total ≤ 100, ticker must exist in `securities`, no duplicate tickers. Tickers are upper-cased
- Whatever is left below 100% is implicitly the cash target
- `IsEnabledForWeekly/BiWeekly/Monthly` are ignored

### Rebalancing

- `PortfolioService` reads the user's targets and fills `TargetAllocationPercentage`, `AllocationDeviation`, `RebalancingAmount`, `RebalancingStatus` per position via `PortfolioCalculator`
- Basis = total portfolio value = Σ position value + cash, the same basis as `CurrentAllocationPercentage`. Position value = market value, falling back to cost basis when no price
- `RebalancingAmount` = € to buy (positive) or sell (negative), rounded to 2 dp (banker's)
- `AllocationDeviation` = rounded current allocation − target; status uses the same rounded current allocation, so the numbers shown and the status always agree
- Deviation ≤ 0.5% → `Balanced`; otherwise `Underweight` / `Overweight`
- Positions without a target keep all four fields `null`
- Targets for securities with no open position don't appear (positions only come from transactions)

### Portfolio History

- Hourly snapshots (Worker): TotalInvested, CashBalance, TotalMarketValue, UnrealizedPnL, RealizedPnL
- Gaps can't be backfilled, so the job keeps running even though no feature depends on it

---

## Securities

- Unique by ticker
- Metadata: SecurityName, SecurityType, ISIN, Currency, Exchange, Sector, Industry, Geography, MarketCap
- `POST` creates-or-gets by ticker via Yahoo search; `POST admin` creates with full metadata for securities Yahoo can't find

---

## Invariants That Must Have Tests

- Sell > shares held → `InvalidOperationException`
- Dividend does not affect FIFO lots or cost basis
- Split multiplies shares in all open lots, price = 0
- Buy cost basis excludes Tax; Sell proceeds don't deduct Tax
- Fully-sold positions excluded from open positions
- Deviation ≤ 0.5% → Balanced; > 0.5% → Underweight / Overweight
- Position without a target → target, deviation, amount and status are `null`
- Allocation targets: each 0–100, total ≤ 100, unknown ticker rejected; never visible across users

---

## Test File Locations

```
Babylon.Alfred.Api.Tests/Features/Investments/
├── Controllers/
├── Services/
└── Shared/      ← Calculator tests (FIFO, realized P&L, splits)
```
