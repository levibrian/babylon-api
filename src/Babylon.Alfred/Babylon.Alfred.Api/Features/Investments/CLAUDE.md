# Investments Feature

## Product Overview

The Investments feature is the core of Babylon Alfred. It enables users to track their investment portfolio across multiple asset types, calculate performance metrics, analyze risk/diversification, and get rebalancing recommendations. This is the largest feature in the system.

## Business Requirements

### Portfolio Tracking
- Users can record Buy, Sell, Dividend, and Split transactions against securities (stocks, ETFs, bonds, crypto, etc.).
- The system calculates portfolio positions by aggregating transactions per security using FIFO (First-In-First-Out) cost basis.
- Portfolio overview includes: total invested (cost basis), total market value, unrealized P&L, and per-position breakdown.
- Positions are ordered by target allocation percentage (descending, nulls last).

### Transaction Management
- Transactions have: SecurityId, TransactionType, Date, SharesQuantity, SharePrice, Fees, Tax.
- On creation: `UpdatedAt` is set to the transaction `Date`. On update: `UpdatedAt` is set to `DateTime.UtcNow`.
- Bulk insert supported for CSV/batch imports.
- Sell validation: cannot sell more shares than currently held (FIFO-aware).
- Realized P&L is calculated on sell transactions: `(Proceeds - Fees - Tax) - CostBasisConsumed`.

### Securities
- Securities are investment instruments identified by unique ticker.
- Metadata includes: SecurityName, SecurityType, ISIN, Currency, Exchange, Sector, Industry, Geography, MarketCap.
- Securities can be created manually or via Yahoo Finance search-and-create (auto-populates metadata).
- Upsert behavior: if ticker exists, update rather than duplicate.

### Allocation Strategies
- Users define target allocation percentages per security.
- One strategy per (UserId, SecurityId) pair (unique constraint).
- Strategies include flags for weekly/bi-weekly/monthly rebalancing enablement.
- Rebalancing status: Balanced (within threshold), Overweight (above), Underweight (below).

### Portfolio Analytics
- **Risk Analysis**: Concentration risk (>20% warning, >40% critical), diversification metrics (HHI), asset count.
- **Income Analysis**: Dividend yield patterns, income stream analysis.
- **Efficiency Analysis**: Return/risk ratios, Sharpe ratio calculations.
- **Trend Analysis**: Price momentum, drawdown alerts.
- Analytics use the `IPortfolioAnalyzer` strategy pattern with four implementations: RiskAnalyzer, IncomeAnalyzer, EfficiencyAnalyzer, TrendAnalyzer.

### Rebalancing
- Standard rebalancing: Calculates buy/sell amounts to reach target allocations.
- Timed rebalancing: Uses 1-year price percentile data for timing (buy when <20th percentile, sell when >80th percentile).
- Smart rebalancing (AI): Delegates to Google Gemini API for optimized recommendations. Feature-flagged, disabled by default.
- Noise threshold: Minimum deviation amount to consider (configurable, default 10).

### Market Prices
- Cached in the `market_prices` table, updated by the Worker service.
- Prices are fetched from Yahoo Finance.
- Market value calculations: `TotalShares * CurrentPrice`.

### Cash Balance
- Tracks user cash holdings separately from investments.
- Updated manually or automatically on transactions.
- Source tracked via `CashUpdateSource` enum (Manual, Transaction).

### Portfolio History
- Hourly snapshots capture: TotalInvested, CashBalance, TotalMarketValue, UnrealizedPnL.
- Used for portfolio performance charts over time.

## Business Rule Invariants (TDD Anchors)

These are the core business rules that MUST have corresponding test coverage:

### Transaction Rules
- Selling more shares than held → must throw `InvalidOperationException` (tested in `TransactionServiceTests`)
- Dividend transaction → does NOT affect FIFO lots or cost basis
- Split transaction → multiplies shares in ALL open lots, price = 0
- Buy cost basis = `(Shares × Price) + Fees` (Tax NOT included)
- Sell realized P&L = `(Proceeds - Fees) - CostBasisConsumed` (Tax NOT deducted from proceeds)
- Tax applies ONLY to Dividend transactions: `NetDividendIncome = GrossAmount - Tax`

### Portfolio Position Rules
- Fully-sold positions (net shares = 0) → excluded from `GetOpenPositionsByUser`
- Open position = security where `SUM(Buy shares) - SUM(Sell shares) > 0` for a user
- Positions ordered by TargetPercentage DESC, nulls last

### Allocation & Rebalancing Rules
- Target allocation deviation < 0.5% → treated as `Balanced` (no rebalancing action)
- Target allocation deviation >= 0.5% → `Underweight` (buy) or `Overweight` (sell)
- Rebalancing noise threshold (default 10) → deviations below this amount are ignored

### Risk Analysis Rules
- Concentration >20% of portfolio → Warning severity
- Concentration >40% of portfolio → Critical severity
- Diversification measured via HHI (Herfindahl-Hirschman Index)

### Analytics Boundary Conditions
- Buy percentile threshold: <20th percentile of 1-year price range
- Sell percentile threshold: >80th percentile of 1-year price range
- Risk-free rate: 3% (US Treasury proxy)
- Trading days per year: 252

### Edge Cases to Test
- Transaction with SecurityId that doesn't exist → throw
- Deleting a Buy that would cause future Sell to over-sell → throw
- Split with ratio 0 or negative → throw
- Dividend with zero shares → allow (stock dividends based on holdings)
- Updating transaction date → `UpdatedAt` set to `DateTime.UtcNow`, not transaction date

## Architecture

### Component Inventory

| Layer | Components | Purpose |
|-------|-----------|---------|
| **Controllers** (11) | Portfolios, Transactions, Securities, Allocation, Analytics, Insights, Rebalancing, History, Market, Cash, User | Thin HTTP layer, delegates to services |
| **Services** (12) | Portfolio, Transaction, Security, AllocationStrategy, Analytics, Insights, Rebalancing, TimedRebalancing, GeminiOptimizer, MarketPrice, CashBalance, History | Business logic orchestration |
| **Analyzers** (4) | Risk, Income, Efficiency, Trend (implement `IPortfolioAnalyzer`) | Strategy pattern for portfolio analysis |
| **Calculators** (4) | Portfolio (FIFO), RealizedPnL, Dividend, Statistics | Pure calculation logic (no dependencies) |
| **Validators** (2) | Transaction, Security | Input validation rules |
| **Mappers** | Transaction | Entity-to-DTO transformation |
| **Models** | Requests/, Responses/ (Portfolios, Analytics, Rebalancing) | DTOs for API contracts |
| **Options** | Configuration classes | Appsettings binding |
| **Extensions** | ServiceCollectionExtensions | DI registration |

### Key Patterns
- **Strategy Pattern**: `IPortfolioAnalyzer` with 4 implementations
- **FIFO Algorithm**: `PortfolioCalculator` maintains cost basis queue
- **Pure Functions**: All calculators and validators have no side effects

## Key Constants

- **Rebalancing threshold**: +/-0.5% deviation from target allocation
- **Risk-free rate**: 3% (US Treasury approximation)
- **Benchmark**: S&P 500 (`^GSPC`)
- **Trading days/year**: 252
- **Concentration warning**: >20%, critical: >40%
- **Buy percentile threshold**: <20th percentile of 1Y range
- **Sell percentile threshold**: >80th percentile of 1Y range

## FIFO Cost Basis Algorithm

1. Maintain a queue of "lots" (buy transactions, ordered by date).
2. **Buy**: Add a new lot with shares and cost (including fees).
3. **Sell**: Consume lots from the front of the queue. Calculate realized P&L as `(SaleProceeds - Fees - Tax) - ConsumedCostBasis`.
4. **Split**: Multiply shares in all existing lots by the split ratio. Price is zero.
5. **Dividend**: Does not affect cost basis. Recorded separately for income tracking.

## Testing

Tests mirror the feature structure under `Babylon.Alfred.Api.Tests/Features/Investments/`:
- Controller tests: Mock services, test HTTP responses.
- Service tests: AutoMocker for DI, mock repositories.
- Analyzer tests: Verify concentration thresholds, diversification metrics, income patterns, trend detection.
- Calculator tests: Verify FIFO, realized P&L, split handling with precise decimal assertions.
