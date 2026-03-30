# Data Model — Babylon Alfred API

## Entities

| Entity | Table | Purpose |
|--------|-------|---------|
| User | `users` | Portfolio owner. Local + Google auth. |
| Security | `securities` | Investment instrument (Stock, ETF, Bond, Crypto, etc.). Unique by ticker. |
| Transaction | `transactions` | Buy/Sell/Dividend/Split records. |
| AllocationStrategy | `allocation_strategies` | Target allocation % per security per user. |
| MarketPrice | `market_prices` | Cached market prices. FK to Security. Updated by Worker. |
| CashBalance | `cash_balances` | User cash holdings. PK = UserId (one per user). |
| PortfolioSnapshot | `portfolio_snapshots` | Hourly portfolio value snapshots for charts. |
| RecurringSchedule | `recurring_schedules` | Recurring investment plans. |
| RefreshToken | `refresh_tokens` | JWT refresh tokens with expiration and revocation. |

---

## Entity Relationships

```
User (1) ──> (N) Transaction
User (1) ──> (N) AllocationStrategy
User (1) ──> (1) CashBalance
User (1) ──> (N) RefreshToken
User (1) ──> (N) PortfolioSnapshot
User (1) ──> (N) RecurringSchedule

Security (1) ──> (N) Transaction
Security (1) ──> (N) AllocationStrategy
Security (1) ──> (1) MarketPrice
Security (1) ──> (N) RecurringSchedule
```

---

## Decimal Precision Rules

| Property | Precision | Scale | Rationale |
|----------|-----------|-------|-----------|
| SharesQuantity | 18 | 8 | Fractional shares (crypto, ETFs) |
| SharePrice, Fees, Tax | 18 | 4 | Standard financial precision |
| MarketPrice.Price | 18 | 4 | Market price precision |
| MarketCap | 20 | 2 | Very large numbers (trillions) |
| TargetPercentage | 8 | 4 | Allocation percentages |
| UnrealizedPnLPercentage | 8 | 4 | P&L percentages |

**These are a contract with the database schema — do not change without a migration.**

---

## Unique Constraints

| Entity | Constraint |
|--------|-----------|
| Security | `Ticker` — unique index |
| AllocationStrategy | `(UserId, SecurityId)` — unique composite index |
| MarketPrice | `SecurityId` — unique (one price per security) |
| CashBalance | `UserId` — primary key |

---

## Computed Properties (NotMapped)

### Transaction.TotalAmount

| Type | Formula | Tax used? |
|------|---------|-----------|
| Buy | `(Shares × Price) + Fees` | **NO** |
| Sell | `(Shares × Price) - Fees` | **NO** |
| Dividend | `(Shares × Price) - Tax` | **YES** — reduces gross to net income |
| Split | `0` | NO (no money changes hands) |

**⚠️ Tax is NEVER included in Buy cost basis or Sell proceeds. Only Dividend.**

### Transaction.Amount (NotMapped)
`Amount = SharesQuantity × SharePrice`

### RefreshToken (NotMapped)
- `IsExpired` = `DateTime.UtcNow >= ExpiresAt`
- `IsActive` = `!IsRevoked && !IsExpired`

---

## SecurityType Enum

`Stock=1, ETF=2, MutualFund=3, Bond=4, Crypto=5, REIT=6, Options=7, Commodity=8, Cash=9`

---

## CashUpdateSource Enum

`Manual, Transaction`

---

## Table Naming

All tables use `snake_case`: `users`, `securities`, `transactions`, `allocation_strategies`, `market_prices`, `cash_balances`, `portfolio_snapshots`, `recurring_schedules`, `refresh_tokens`.

---

## Migration Rules (CRITICAL — production is irreversible)

- **NEVER** drop a column in a single migration → add new → backfill → verify → drop in separate migration
- **NEVER** rename a column in a single migration → add new → backfill → update code → drop old
- **NEVER** change column types without explicit casting
- **ALWAYS** add new columns as nullable first, then backfill, then add NOT NULL constraint if needed
- **ALWAYS** run `dotnet ef migrations script` and inspect SQL before applying
- **ALWAYS** test migrations against local PostgreSQL before committing
- **ALWAYS** implement `Down()` method (reversible migrations)
- **NEVER** modify migrations already applied to production

### Migration Workflow

```
1. Make entity changes in code
2. dotnet ef migrations add MigrationName
3. dotnet ef migrations script   ← inspect SQL
4. dotnet ef database update     ← test locally
5. Verify data integrity
6. Commit migration file
7. Deploy (auto-applies on startup via context.Database.Migrate())
```

---

## Database Provider

PostgreSQL via Npgsql. Retry on failure: 3 retries, 5-second max delay.

---

## Repository Inventory

| Interface | Entity | Key Operations |
|-----------|--------|---------------|
| `IUserRepository` | User | GetByUsername, GetByEmail, GetById, Create, Update |
| `ITransactionRepository` | Transaction | Add, AddBulk, GetAll, GetAllByUser, **GetOpenPositionsByUser**, GetById, Update, Delete |
| `ISecurityRepository` | Security | GetByTicker, GetByIsin, GetByIds, GetAll, AddOrUpdate, Delete |
| `IMarketPriceRepository` | MarketPrice | GetByTicker, GetByTickers, UpsertMarketPriceAsync, MarkAsNotFound, GetSecuritiesNeedingUpdate |
| `IAllocationStrategyRepository` | AllocationStrategy | GetByUserId, SetStrategy, GetDistinctSecurityIds |
| `ICashBalanceRepository` | CashBalance | GetByUserId, AddOrUpdate |
| `IPortfolioSnapshotRepository` | PortfolioSnapshot | AddSnapshotAsync, GetByUserAndDateRange, GetUserIdsWithPortfoliosAsync |
| `IRecurringScheduleRepository` | RecurringSchedule | GetActiveByUserId, CreateOrUpdate, Delete |
| `IRefreshTokenRepository` | RefreshToken | GetByToken, Add, Update, RevokeAllByUserId |

### GetOpenPositionsByUser

Returns only transactions for securities where the user still holds shares.

**Definition**: Open position = security where `SUM(Buy shares) - SUM(Sell shares) > 0` for a given user.

Used by portfolio calculation to exclude fully-sold positions.

---

## Adding a New Entity — Checklist

1. Create model class in `Shared/Data/Models/`
2. Create Fluent API configuration in `Shared/Data/Configurations/`
3. Add `DbSet<T>` to `BabylonDbContext`
4. Create migration: `dotnet ef migrations add MigrationName`
5. Create `I{Entity}Repository` + `{Entity}Repository` in `Shared/Repositories/`
6. Register repository in DI as Scoped
