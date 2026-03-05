# Shared/Repositories - Data Access Layer

## Overview

All database access goes through repository interfaces. Implementations use EF Core's `BabylonDbContext`.

**DI lifetime**: See root `CLAUDE.md` § Global Rules. Default is **Scoped** (per-request).

## Repository Inventory

| Interface | Implementation | Entity | Key Operations |
|-----------|---------------|--------|---------------|
| `IUserRepository` | `UserRepository` | User | GetByUsername, GetByEmail, GetById, Create, Update |
| `ITransactionRepository` | `TransactionRepository` | Transaction | Add, AddBulk, GetAll, GetAllByUser, GetOpenPositionsByUser, GetById, Update, Delete |
| `ISecurityRepository` | `SecurityRepository` | Security | GetByTicker, GetByIsin, GetByIds, GetAll, AddOrUpdate, Delete |
| `IMarketPriceRepository` | `MarketPriceRepository` | MarketPrice | GetByTicker, GetByTickers, UpsertMarketPriceAsync, MarkAsNotFound, GetSecuritiesNeedingUpdate |
| `IAllocationStrategyRepository` | `AllocationStrategyRepository` | AllocationStrategy | GetByUserId, SetStrategy, GetDistinctSecurityIds |
| `ICashBalanceRepository` | `CashBalanceRepository` | CashBalance | GetByUserId, AddOrUpdate |
| `IPortfolioSnapshotRepository` | `PortfolioSnapshotRepository` | PortfolioSnapshot | AddSnapshotAsync, GetByUserAndDateRange, GetUserIdsWithPortfoliosAsync |
| `IRecurringScheduleRepository` | `RecurringScheduleRepository` | RecurringSchedule | GetActiveByUserId, CreateOrUpdate, Delete |
| `IRefreshTokenRepository` | `RefreshTokenRepository` | RefreshToken | GetByToken, Add, Update, RevokeAllByUserId |

## Conventions

- Interface methods use `Async` suffix.
- All methods are async (return `Task<T>`).
- Repositories do NOT contain business logic - only data access.
- Repositories do NOT call other repositories.
- Navigation properties are loaded eagerly via `.Include()` where needed.
- Bulk operations use `EFCore.BulkExtensions` where appropriate.

## GetOpenPositionsByUser

Key method on `ITransactionRepository` that returns only transactions for securities where the user still holds shares.

**Definition**: Open position = security where `SUM(Buy shares) - SUM(Sell shares) > 0` for a given user.

Used by portfolio calculation to avoid processing closed positions (fully sold securities).

## Adding a New Repository

1. Create interface `I{Entity}Repository` in `Shared/Repositories/`.
2. Create implementation `{Entity}Repository` in `Shared/Repositories/`.
3. Inject `BabylonDbContext` via constructor.
4. Register in `ServiceCollectionExtensions` as Scoped.
5. Consume via interface injection in service layer.
