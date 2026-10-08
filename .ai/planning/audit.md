# Feature audit

Judged against the north star in `../decisions.md`. One pass, not a revamp.

| Decision | Items |
|---|---|
| **Keep** | Transactions · Securities (incl. `POST /admin`, the non-Yahoo fallback) · Portfolios + FIFO calculators · Cash · Yahoo price worker · Yahoo search · Snapshots + history (data collection; gaps can't be backfilled) · Realized PnL + backfill · Auth (simplify during the OAuth step) · `AllocationStrategy` table (also drives which prices are fetched) · `RecurringSchedule` table (no work now) |
| **Cut (done)** | `HistoricalPriceService` (registered, never called) · `WeatherForecast.cs` · `Features/BrokerIntegration/` spec (parked in `ideas.md`) · `.ai/features/` analyzers, telegram, recurring-schedules docs · stale `.claude/settings.local.json` |
| **No table drops** | Both unwired tables are kept by decision; dropping them loses stored data. |

## Follow-up
- `investments.md`, `architecture.md`, `testing.md`, `data-model.md` and `README.md` still describe removed controllers and services (Analytics, Insights, Rebalancing, GeminiOptimizer, Telegram, repositories that don't exist). These docs are loaded into every session via `CLAUDE.md`.
