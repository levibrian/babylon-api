# Features Index — Babylon Alfred API

## Feature Map

| Feature | Status | File | Summary |
|---------|--------|------|---------|
| **Investments** | Production | [investments.md](investments.md) | Core feature. Portfolio tracking, transactions (Buy/Sell/Dividend/Split), securities, cash, FIFO cost basis, realized PnL, portfolio history. Rebalancing math exists in `PortfolioCalculator` but is not wired up yet. |
| **Authentication** | Production | [authentication.md](authentication.md) | JWT + Google OAuth. Unified accounts (one email = one user). Refresh tokens, account linking. |
| **Worker** | Production | [worker.md](worker.md) | Quartz.NET background jobs. Price fetching (hourly), portfolio snapshots (hourly+15m), realized PnL backfill (daily 3AM). |
| **Infrastructure** | Production | [infrastructure.md](infrastructure.md) | Yahoo Finance integration: security search and metadata. |
| **Startup** | Internal | _(see architecture.md)_ | Health check endpoint, root DI registration (`RegisterFeatures()`). |

---

## Cross-Feature Rules

- Features do **NOT** depend on each other
- Cross-cutting logic only goes in `Shared/`
- New features must be wired into `Features/Startup/Extensions/ServiceCollectionExtensions.RegisterFeatures()`
- New features must get a file in `.ai/features/`
