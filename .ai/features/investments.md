# Investments Feature

## Overview

Core feature of Babylon Alfred. Enables users to track their investment portfolio across multiple asset types, calculate performance metrics, analyze risk/diversification, and get rebalancing recommendations. This is the largest feature in the system.

---

## Component Inventory

| Layer | Components | Purpose |
|-------|-----------|---------|
| Controllers (11) | Portfolios, Transactions, Securities, Allocation, Analytics, Insights, Rebalancing, History, Market, Cash, User | Thin HTTP layer |
| Services (12) | Portfolio, Transaction, Security, AllocationStrategy, Analytics, Insights, Rebalancing, TimedRebalancing, GeminiOptimizer, MarketPrice, CashBalance, History | Business logic orchestration |
| Analyzers (4) | Risk, Income, Efficiency, Trend | Strategy pattern — see [analyzers.md](analyzers.md) |
| Calculators (4) | Portfolio (FIFO), RealizedPnL, Dividend, Statistics | Pure calculation logic (no dependencies) |
| Validators (2) | Transaction, Security | Input validation |
| Mappers | Transaction | Entity-to-DTO transformation |

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

- **DO NOT** include `Tax` in Buy lot `TotalCost`. Formula: `(Shares × Price) + Fees` only.
- **DO NOT** deduct `Tax` from Sell `NetProceeds`. Formula: `(Shares × Price) - Fees` only.
- **DO NOT** apply `Tax` to Buy or Sell `TotalAmount`. Tax is exclusively a Dividend concern.
- **DO NOT** add a new transaction type without updating: FIFO algorithm, `TotalAmount` switch, Tax applicability table, and this file.
- **DO NOT** add `Fees` to Dividend cost basis or treat Dividend as a purchase.
- **DO NOT** recalculate `RealizedPnL` on the `Transaction` entity directly from the service layer — use `RealizedPnLCalculator` or `PortfolioCalculator`.

### Transaction Creation Rules

- On **creation**: `UpdatedAt` is set to the transaction `Date`
- On **update**: `UpdatedAt` is set to `DateTime.UtcNow`
- **Sell validation**: Cannot sell more shares than currently held (FIFO-aware)
- Bulk insert supported for CSV/batch imports

### Portfolio Position Rules

- Fully-sold positions (net shares = 0) → excluded from `GetOpenPositionsByUser`
- Open position = security where `SUM(Buy shares) - SUM(Sell shares) > 0`
- Positions ordered by TargetPercentage DESC, nulls last

### Allocation & Rebalancing Rules

- Target allocation deviation **< 0.5%** → `Balanced` (no action)
- Target allocation deviation **≥ 0.5%** → `Underweight` (buy) or `Overweight` (sell)
- Noise threshold (default 10) → deviations below this dollar amount are ignored
- One strategy per `(UserId, SecurityId)` pair (unique constraint)

### Market Prices & Cash

- Market prices cached in `market_prices`, updated by the Worker service (hourly)
- Market value calculation: `TotalShares × CurrentPrice`
- Cash balance tracked separately from investments

---

## Analytics

### Risk Analysis (RiskAnalyzer)
- Concentration **>20%** of portfolio → Warning severity
- Concentration **>40%** of portfolio → Critical severity
- Diversification via HHI (Herfindahl-Hirschman Index)

### Rebalancing Modes
- **Standard**: Buy/sell amounts to reach target allocations
- **Timed**: Uses 1-year price percentile data (buy <20th percentile, sell >80th percentile)
- **Smart AI (Gemini)**: Feature-flagged, disabled by default

### Constants
- Risk-free rate: **3%** (US Treasury approximation)
- Benchmark: **S&P 500 (`^GSPC`)**
- Trading days/year: **252**
- Buy percentile threshold: **<20th** of 1Y range
- Sell percentile threshold: **>80th** of 1Y range

---

## Business Rule Invariants (TDD Anchors)

These MUST have test coverage:

### Transaction Rules
- Sell > shares held → `InvalidOperationException`
- Dividend → does NOT affect FIFO lots or cost basis
- Split → multiplies shares in ALL open lots, price = 0
- Buy cost basis = `(Shares × Price) + Fees` (Tax NOT included)
- Sell realized P&L = `(Proceeds - Fees) - CostBasisConsumed` (Tax NOT deducted)
- Tax applies ONLY to Dividends: `NetDividendIncome = GrossAmount - Tax`

### Portfolio Rules
- Fully-sold positions excluded from `GetOpenPositionsByUser`
- Positions ordered by TargetPercentage DESC, nulls last

### Allocation Rules
- Deviation < 0.5% → Balanced
- Deviation ≥ 0.5% → Underweight/Overweight

### Edge Cases to Test
- Transaction with non-existent SecurityId → throw
- Deleting a Buy that would cause future Sell to over-sell → throw
- Split with ratio 0 or negative → throw
- Dividend with zero shares → allow (stock dividends based on holdings)
- Updating transaction date → `UpdatedAt` set to `DateTime.UtcNow`

---

## Securities

- Unique by ticker
- Metadata: SecurityName, SecurityType, ISIN, Currency, Exchange, Sector, Industry, Geography, MarketCap
- Can be created manually or via Yahoo Finance search-and-create (auto-populates metadata)
- Upsert behavior: if ticker exists, update rather than duplicate

---

## Portfolio History

- Hourly snapshots: TotalInvested, CashBalance, TotalMarketValue, UnrealizedPnL
- Used for portfolio performance charts over time

---

## Test File Locations

```
Babylon.Alfred.Api.Tests/Features/Investments/
├── Controllers/
├── Services/
├── Analyzers/   ← concentration thresholds, diversification metrics
└── Shared/      ← Calculator tests (FIFO, realized P&L, splits, dividends)
```
