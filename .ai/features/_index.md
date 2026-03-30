# Features Index — Babylon Alfred API

## Feature Map

| Feature | Status | File | Summary |
|---------|--------|------|---------|
| **Investments** | Production | [investments.md](investments.md) | Core feature. Portfolio tracking, transactions (Buy/Sell/Dividend/Split), securities, FIFO cost basis, analytics, rebalancing. Largest feature. |
| **Authentication** | Production | [authentication.md](authentication.md) | JWT + Google OAuth. Unified accounts (one email = one user). Refresh tokens, account linking. |
| **RecurringSchedules** | Production | [recurring-schedules.md](recurring-schedules.md) | Recurring investment plans per security. Upsert semantics on (UserId, SecurityId). |
| **Analyzers** | Production | [analyzers.md](analyzers.md) | IPortfolioAnalyzer strategy pattern. 4 implementations: Risk, Income, Efficiency, Trend. |
| **Worker** | Production | [worker.md](worker.md) | Quartz.NET background jobs. Price fetching (hourly), portfolio snapshots (hourly+15m), realized PnL backfill (daily 3AM). |
| **Infrastructure** | Production | [infrastructure.md](infrastructure.md) | Yahoo Finance integration. Market data search + historical prices. |
| **Telegram** | WIP / Scaffolded | [telegram.md](telegram.md) | Bot client registered in DI. Stub controller. No business logic. Requires spec before any implementation. |
| **Startup** | Internal | _(see architecture.md)_ | Health check endpoint, root DI registration (`RegisterFeatures()`). |

---

## Cross-Feature Rules

- Features do **NOT** depend on each other
- Cross-cutting logic only goes in `Shared/`
- New features must be wired into `Features/Startup/Extensions/ServiceCollectionExtensions.RegisterFeatures()`
- New features must get a file in `.ai/features/`
