# Babylon Alfred - Solution Context

## Business Context

Babylon is a personal investment portfolio management platform. Named after Batman's butler Alfred, the system serves as an automated assistant that tracks stock portfolio transactions, manages positions, calculates portfolio metrics, and provides rebalancing recommendations. The frontend is a React application deployed at `babylonfinance.vercel.app`.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Language | C# 12, .NET 9.0 |
| Web Framework | ASP.NET Core |
| ORM | Entity Framework Core 8.0 |
| Database | PostgreSQL 16 (Npgsql) |
| Logging | Serilog (structured, Console + File sinks) |
| API Docs | Swagger / Swashbuckle |
| Auth | JWT Bearer + Google OAuth + BCrypt |
| Scheduling | Quartz.NET |
| External Data | Yahoo Finance API |
| AI | Google Gemini 2.5 Flash (feature-flagged) |
| Messaging | Telegram Bot SDK (in progress) |
| Testing | xUnit, Moq, AutoMoq, AutoFixture, FluentAssertions, EF Core InMemory |
| IaC | Terraform (AWS VPC, RDS, Secrets Manager) |
| Deploy | Fly.io (Docker containers), region: CDG (Paris) |
| CI/CD | GitHub Actions (build + test on push/PR to main) |

## Solution Structure

```
babylon-api/
├── src/Babylon.Alfred/
│   ├── Babylon.Alfred.sln
│   ├── Babylon.Alfred.Api/          # REST API (main application)
│   ├── Babylon.Alfred.Api.Tests/    # Unit + integration tests
│   ├── Babylon.Alfred.Worker/       # Background job service
│   └── docker-compose.yml
├── iac/                             # Terraform infrastructure
├── .github/workflows/               # CI/CD pipelines
├── fly.api.toml                     # Fly.io API deployment config
├── fly.db.prod.toml                 # Fly.io database config
└── test-api.http                    # HTTP request samples
```

### Projects

- **Babylon.Alfred.Api**: REST API serving the investment platform. Vertical slice architecture with feature folders. Contains domain models, repositories, services, and controllers.
- **Babylon.Alfred.Worker**: .NET hosted service running scheduled background jobs via Quartz.NET. Fetches market prices, creates portfolio snapshots, and runs data backfills. References the API project for shared models and business logic.
- **Babylon.Alfred.Api.Tests**: xUnit test project mirroring the API folder structure. Covers controllers, services, analyzers, calculators, and repositories.

## Architecture

### Vertical Slice Architecture

Features are organized into self-contained folders under `Features/`. Each feature owns its controllers, services, models (requests/responses), and DI registration extensions. Cross-cutting concerns live in `Shared/` (data layer, repositories, middleware, logging).

### Layering Within Features

```
Controller -> Service -> Repository -> DbContext
                 |
                 v
            Calculator / Validator / Mapper (shared utilities)
```

- **Controllers**: Thin. Delegate to services. Return `ApiResponse<T>` or `ApiErrorResponse`.
- **Services**: Business logic. Orchestrate repositories, calculators, and validators.
- **Repositories**: Data access via EF Core. Interface + implementation pattern. Registered as Scoped.
- **Shared Utilities**: Pure calculation logic (PortfolioCalculator, RealizedPnLCalculator, etc.).

### API Response Envelope

All API responses use a standard envelope:
- Success: `ApiResponse<T>` with `{ Success: true, Data: T }`
- Error: `ApiErrorResponse` with `{ Success: false, Message: string, Errors: [] }`

### Middleware Pipeline

1. CORS (early, for preflight requests)
2. `RequestLoggingMiddleware` (logs all HTTP requests with timing)
3. `GlobalErrorHandlerMiddleware` (catches unhandled exceptions, returns `ApiErrorResponse`)
4. Swagger
5. Authorization
6. Controller routing

## Design Patterns

- **Repository Pattern**: All data access goes through `I*Repository` interfaces. Implementations are scoped-lifetime, injected via DI.
- **Service Pattern**: Business logic encapsulated in `I*Service` interfaces. Services never access `DbContext` directly.
- **Vertical Slice**: Each feature is self-contained. Features register their own DI via `ServiceCollectionExtensions`.
- **Strategy Pattern**: `IPortfolioAnalyzer` interface implemented by multiple analyzers (Risk, Income, Efficiency, Trend).
- **Feature Flags**: AI rebalancing (Gemini) is feature-flagged via configuration.
- **FIFO Cost Basis**: Portfolio calculations use First-In-First-Out method for cost basis and realized P&L.

## Domain Model Summary

| Entity | Table | Purpose |
|--------|-------|---------|
| User | `users` | Portfolio owner. Supports local + Google auth. |
| Security | `securities` | Investment instrument (Stock, ETF, Bond, Crypto, etc.). Unique by ticker. |
| Transaction | `transactions` | Buy/Sell/Dividend/Split records with shares, price, fees, tax, realized P&L. |
| AllocationStrategy | `allocation_strategies` | Target allocation % per security per user. Unique constraint on (UserId, SecurityId). |
| MarketPrice | `market_prices` | Cached market prices. FK to Security. Updated by Worker. |
| CashBalance | `cash_balances` | User cash holdings. One-to-one with User. PK is UserId. |
| PortfolioSnapshot | `portfolio_snapshots` | Hourly portfolio value snapshots for historical tracking. |
| RecurringSchedule | `recurring_schedules` | Automated recurring investment plans. |
| RefreshToken | `refresh_tokens` | JWT refresh tokens with expiration and revocation. |

## Key Business Rules

- **FIFO Cost Basis**: Sell transactions consume the oldest buy lots first. Splits multiply shares across all lots. Dividends do not affect cost basis.
- **Buy Cost Basis**: `(Shares × Price) + Fees`. Tax is NOT included.
- **Realized P&L**: Calculated on sell transactions as `(Proceeds - Fees) - CostBasisConsumed`. Tax is NOT deducted from sell proceeds.
- **Tax**: Applies ONLY to Dividend transactions — `NetDividendIncome = GrossAmount - Tax`. DO NOT use Tax in Buy cost basis or Sell proceeds calculations.
- **Rebalancing Threshold**: Positions within +/-0.5% of target are considered balanced.
- **Transaction Ordering**: Always by `UpdatedAt DESC`.
- **Root User ID**: `a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d` (legacy, being phased out with multi-user auth).

## DO NOT Rules

- **DO NOT** include `Tax` in `TotalCost` for Buy lots: formula is `(Shares × Price) + Fees`.
- **DO NOT** deduct `Tax` from Sell net proceeds: formula is `(Shares × Price) - Fees`.
- **DO NOT** use `Tax` in `Transaction.TotalAmount` for Buy or Sell types; only `Dividend.TotalAmount` deducts Tax.
- **DO NOT** add new transaction types without updating the FIFO algorithm, `TotalAmount`, and Tax applicability rules.

## Code Conventions

- **Nullable reference types**: Enabled solution-wide.
- **Async methods**: Service methods do NOT use `Async` suffix. Repository methods DO use `Async` suffix.
- **Indentation**: 4 spaces for C#, 2 spaces for JSON/YAML.
- **Braces**: All on new line (Allman style).
- **Line endings**: CRLF.
- **Enums**: Serialized as strings via `StringEnumConverter`.
- **Decimal precision**: Shares (18,8), Prices/Fees/Tax (18,4), MarketCap (20,2), Percentages (8,4).

## Testing Conventions

- **Framework**: xUnit with `[Fact]` and `[Theory]`/`[InlineData]`.
- **Naming**: `MethodName_ScenarioCondition_ExpectedResult`.
- **Mocking**: Moq + AutoMocker (`autoMocker.CreateInstance<T>()`).
- **Data generation**: AutoFixture with customizations for DateOnly and recursive types.
- **Assertions**: FluentAssertions (`result.Should().Be(expected)`).
- **Database tests**: EF Core InMemory with unique database per test (`Guid.NewGuid().ToString()`).
- **Controller tests**: Mock `ControllerContext` with `ClaimsPrincipal` for auth scenarios.
- **Pattern**: Strict AAA (Arrange-Act-Assert) with clear separation.
- **GUID generation**: Use `Guid.NewGuid()`, never `fixture.Create<Guid>()`.

## Deployment

- **API**: Fly.io, 1 shared CPU, 512MB RAM, port 8080, HTTPS enforced, health check at `/health`.
- **Database**: Fly.io managed Postgres, 1 shared CPU, 1024MB RAM, always-on.
- **Docker**: Multi-stage build. Base: `mcr.microsoft.com/dotnet/aspnet:9.0`. Entry: `Babylon.Alfred.Api.dll`.
- **CI/CD**: GitHub Actions on push/PR to `main`. Build + test in Release mode.

## Authentication

- **JWT**: HS256, 24-hour expiration, zero clock skew. Claims: Sub (user ID), Email, UniqueName, AuthProvider.
- **Refresh Tokens**: 7-day expiration, revoked on logout, previous tokens revoked on new login.
- **Google OAuth**: Validates IdToken, auto-creates/links user accounts.
- **Password**: BCrypt hashing for local auth.
- **CORS**: Allowed origins configurable. Defaults: `localhost:3000`, `localhost:3001`, `babylonfinance.vercel.app`.
