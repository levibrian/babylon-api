# Shared/Data - Data Layer

## Overview

Contains the EF Core DbContext, all domain entity models, Fluent API configurations, and database migrations. This is the single source of truth for the database schema.

## Structure

```
Shared/Data/
├── BabylonDbContext.cs              # EF Core DbContext with all DbSets
├── Models/                          # Domain entities
│   ├── User.cs                      # Portfolio owner (local + Google auth)
│   ├── Security.cs                  # Investment instrument (Stock, ETF, Bond, etc.)
│   ├── SecurityType.cs              # Enum: Stock=1, ETF=2, MutualFund=3, Bond=4, Crypto=5, REIT=6, Options=7, Commodity=8, Cash=9
│   ├── Transaction.cs               # Buy/Sell/Dividend/Split record
│   ├── AllocationStrategy.cs        # Target allocation % per security per user
│   ├── MarketPrice.cs               # Cached market price (FK to Security)
│   ├── CashBalance.cs               # User cash holdings (PK = UserId)
│   ├── CashUpdateSource.cs          # Enum: Manual, Transaction
│   ├── PortfolioSnapshot.cs         # Hourly portfolio value snapshot
│   ├── RecurringSchedule.cs         # Recurring investment plan
│   └── RefreshToken.cs              # JWT refresh token with expiration/revocation
├── Configurations/                  # EF Core Fluent API entity configurations
│   ├── UserConfiguration.cs
│   ├── SecurityConfiguration.cs
│   ├── TransactionConfiguration.cs
│   ├── AllocationStrategyConfiguration.cs
│   ├── MarketPriceConfiguration.cs
│   ├── CashBalanceConfiguration.cs
│   ├── PortfolioSnapshotConfiguration.cs
│   ├── RecurringScheduleConfiguration.cs
│   └── RefreshTokenConfiguration.cs
├── Migrations/                      # EF Core code-first migrations
└── Scripts/                         # Manual SQL scripts for data operations
```

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

## Decimal Precision Rules

| Property | Precision | Scale | Rationale |
|----------|-----------|-------|-----------|
| SharesQuantity | 18 | 8 | Supports fractional shares (crypto, ETFs) |
| SharePrice, Fees, Tax | 18 | 4 | Standard financial precision |
| MarketPrice.Price | 18 | 4 | Market price precision |
| MarketCap | 20 | 2 | Very large numbers (trillions) |
| TargetPercentage | 8 | 4 | Allocation percentages |
| UnrealizedPnLPercentage | 8 | 4 | P&L percentages |

## Key Constraints

- `Security.Ticker`: Unique index.
- `AllocationStrategy.(UserId, SecurityId)`: Unique composite index.
- `MarketPrice.SecurityId`: Unique (one price per security).
- `CashBalance.UserId`: Primary key (one balance per user).
- `Transaction.UserId`: Nullable FK (legacy: defaults to RootUserId in service layer).

## Computed Properties (NotMapped)

**Transaction:**
- `Amount` = `SharesQuantity * SharePrice`
- `TotalAmount`: Varies by type:
  - Buy: `Amount + Fees + Tax`
  - Sell: `Amount - Fees - Tax`
  - Dividend: `(SharesQuantity * SharePrice) - Tax` (gross - tax = net income)
  - Split: `0` (no money involved)

**RefreshToken:**
- `IsExpired` = `DateTime.UtcNow >= ExpiresAt`
- `IsActive` = `!IsRevoked && !IsExpired`

## Table Names

All tables use snake_case: `users`, `securities`, `transactions`, `allocation_strategies`, `market_prices`, `cash_balances`, `portfolio_snapshots`, `recurring_schedules`, `refresh_tokens`.

## Migration Rules

**Critical**: Migrations are irreversible in production. Follow these rules to prevent data loss:

- **NEVER** drop columns in a single migration. Process: add new column → backfill data → verify → drop old column in separate migration.
- **NEVER** rename columns in a single migration. Process: add new column → backfill → update code → drop old column.
- **NEVER** change column types without explicit casting. PostgreSQL may reject implicit conversions.
- **ALWAYS** add new columns as nullable first, then backfill, then add NOT NULL constraint if needed.
- **ALWAYS** run `dotnet ef migrations script` and inspect SQL before applying.
- **ALWAYS** test migrations against a local PostgreSQL instance before committing.
- **ALWAYS** ensure migrations are reversible (implement `Down()` method).
- **NEVER** modify existing migrations that have been applied to production.

### Migration Workflow
1. Make entity changes in code
2. Run: `dotnet ef migrations add MigrationName`
3. Inspect generated SQL: `dotnet ef migrations script`
4. Test on local PostgreSQL: `dotnet ef database update`
5. Verify data integrity
6. Commit migration file
7. Deploy (migrations auto-apply on startup via `context.Database.Migrate()`)

## Adding a New Entity

1. Create model class in `Models/`.
2. Create Fluent API configuration in `Configurations/`.
3. Add `DbSet<T>` to `BabylonDbContext`.
4. Create and apply migration: `dotnet ef migrations add MigrationName`.
5. Create repository interface + implementation in `Shared/Repositories/`.
6. Register repository in DI.

## Database Provider

PostgreSQL via Npgsql. Retry on failure configured: 3 retries, 5-second max delay.

## Invariants (Do Not Change Without Discussion)

- **Decimal precision rules** (§ Decimal Precision Rules table) are contract with database schema
- **Table names** must remain snake_case for consistency with existing migrations
- **Foreign key relationships** (§ Entity Relationships) define referential integrity constraints
- **Computed properties** (`[NotMapped]`) are derived fields — changing them may break API contracts
