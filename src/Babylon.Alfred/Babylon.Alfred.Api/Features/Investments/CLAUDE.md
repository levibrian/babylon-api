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
- Realized P&L is calculated on sell transactions: `(Proceeds - Fees) - CostBasisConsumed`. Tax is NOT deducted from proceeds.
- **Tax field** is stored on all transaction types but is only applied in Dividend income calculations. It is ignored for Buy and Sell financial computations.

### `Transaction.TotalAmount` (computed, not persisted)
| Type | Formula | Tax used? |
|------|---------|-----------|
| Buy | `(Shares × Price) + Fees` | NO |
| Sell | `(Shares × Price) - Fees` | NO |
| Dividend | `(Shares × Price) - Tax` | YES — reduces gross to net income |
| Split | `0` | NO |

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

## Architecture

```
Features/Investments/
├── Controllers/           # 11 controllers (thin, delegate to services)
│   ├── PortfoliosController        # GET portfolio with positions & allocations
│   ├── TransactionsController      # CRUD transactions + bulk
│   ├── SecuritiesController        # CRUD securities + search-and-create
│   ├── AllocationController        # GET/SET allocation strategies
│   ├── PortfolioAnalyticsController # Risk, income, efficiency metrics
│   ├── InsightsController          # AI-powered portfolio insights
│   ├── RebalancingController       # Rebalancing suggestions + smart rebalancing
│   ├── PortfolioHistoryController  # Historical snapshots
│   ├── MarketController            # Market price data
│   ├── CashController              # Cash balance management
│   └── UserController              # User profile
├── Services/              # Business logic layer
│   ├── PortfolioService            # Position aggregation, cost basis calculation
│   ├── TransactionService          # Transaction CRUD + validation + realized P&L
│   ├── SecurityService             # Security CRUD + Yahoo Finance integration
│   ├── AllocationStrategyService   # Allocation target management
│   ├── PortfolioAnalyticsService   # Orchestrates analyzer pipeline
│   ├── PortfolioInsightsService    # Runs all analyzers, collects insights
│   ├── RebalancingService          # Rebalancing action calculation
│   ├── TimedRebalancingActionsService # Time-based buy/sell signals
│   ├── GeminiRebalancingOptimizer  # AI-powered rebalancing (Gemini API)
│   ├── MarketPriceService          # Reads cached market prices
│   ├── CashBalanceService          # Cash balance management
│   └── PortfolioHistoryService     # Snapshot queries
├── Analyzers/             # Strategy pattern implementations
│   ├── IPortfolioAnalyzer          # Interface: AnalyzeAsync(portfolio, history)
│   ├── RiskAnalyzer                # Concentration, diversification, HHI
│   ├── IncomeAnalyzer              # Dividend patterns
│   ├── EfficiencyAnalyzer          # Sharpe ratio, return/risk
│   └── TrendAnalyzer               # Momentum, drawdown
├── Shared/                # Shared utilities within Investments feature
│   ├── PortfolioCalculator         # FIFO cost basis, position metrics, allocations
│   ├── RealizedPnLCalculator       # Realized P&L per transaction (FIFO lots)
│   ├── DividendCalculator          # Gross/net dividend calculations
│   ├── StatisticsCalculator        # Portfolio statistics
│   ├── TransactionValidator        # Input validation rules
│   ├── SecurityValidator           # Security existence validation
│   ├── TransactionMapper           # Entity-to-DTO mapping
│   ├── ErrorMessages               # Centralized error message strings
│   └── RiskMetricsConstants        # Benchmark ticker, risk-free rate, trading days
├── Models/
│   ├── Requests/                   # Input DTOs (CreateTransactionRequest, etc.)
│   └── Responses/                  # Output DTOs
│       ├── Portfolios/             # PortfolioResponse, PortfolioPositionDto, etc.
│       ├── Analytics/              # RiskMetricsDto, DiversificationMetricsDto
│       └── Rebalancing/            # RebalancingActionDto, SmartRebalancingResponseDto
├── Options/               # Configuration option classes
└── Extensions/            # DI registration for this feature
```

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
2. **Buy**: Add a new lot. `TotalCost = (Shares × Price) + Fees`. Tax is NOT included in cost basis.
3. **Sell**: Consume lots from the front of the queue. `NetProceeds = (Shares × Price) - Fees`. Tax is NOT deducted. Realized P&L = `NetProceeds - ConsumedCostBasis`.
4. **Split**: Multiply shares in all existing lots by the split ratio. Price is zero. No money changes hands.
5. **Dividend**: Does not affect cost basis. Net income = `GrossAmount - Tax`. Tax IS applied here only.

## DO NOT Rules

- **DO NOT** include `Tax` in Buy lot `TotalCost`. Formula: `(Shares × Price) + Fees` only.
- **DO NOT** deduct `Tax` from Sell `NetProceeds`. Formula: `(Shares × Price) - Fees` only.
- **DO NOT** apply `Tax` to Buy or Sell `TotalAmount`. Tax is exclusively a Dividend concern.
- **DO NOT** add a new transaction type without updating: FIFO algorithm, `TotalAmount` switch, Tax applicability table, and this CLAUDE.md.
- **DO NOT** add `Fees` to Dividend cost basis or treat Dividend as a purchase.
- **DO NOT** recalculate `RealizedPnL` on the `Transaction` entity directly from the service layer — use `RealizedPnLCalculator` or `PortfolioCalculator` (calculators own this logic).

## Testing

Tests mirror the feature structure under `Babylon.Alfred.Api.Tests/Features/Investments/`:
- Controller tests: Mock services, test HTTP responses.
- Service tests: AutoMocker for DI, mock repositories.
- Analyzer tests: Verify concentration thresholds, diversification metrics, income patterns, trend detection.
- Calculator tests: Verify FIFO, realized P&L, split handling with precise decimal assertions.
