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

**Structural Principles**:
- **Vertical slices** under `Features/` (self-contained feature modules with Controllers, Services, Models, Extensions)
- **Cross-cutting concerns** in `Shared/` (Data layer, Repositories, Middlewares, Logging, Models, Extensions)
- **External adapters** in `Infrastructure/` (Yahoo Finance, etc.)
- **Feature folders** contain: `Controllers/`, `Services/`, `Models/Requests/`, `Models/Responses/`, `Extensions/ServiceCollectionExtensions.cs`

### Projects

- **Babylon.Alfred.Api**: REST API (`src/Babylon.Alfred/Babylon.Alfred.Api/`). Vertical slice architecture. Domain models, repositories, services, controllers.
- **Babylon.Alfred.Worker**: Background job service (`src/Babylon.Alfred/Babylon.Alfred.Worker/`). Quartz.NET scheduler. Fetches market prices, creates snapshots, runs backfills. References API project.
- **Babylon.Alfred.Api.Tests**: xUnit test project (`src/Babylon.Alfred/Babylon.Alfred.Api.Tests/`). Mirrors API structure. Controllers, services, analyzers, calculators, repositories.

### Key Anchor Files
- `babylon-api/iac/` - Terraform infrastructure (AWS VPC, RDS, Secrets)
- `babylon-api/.github/workflows/` - CI/CD pipelines
- `babylon-api/fly.api.toml` - Fly.io API deployment config
- `babylon-api/test-api.http` - HTTP request samples

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

## Testing Contract (TDD)

### 🚨 MANDATORY TDD WORKFLOW 🚨

**YOU MUST ALWAYS FOLLOW TEST-DRIVEN DEVELOPMENT. NO EXCEPTIONS.**

When implementing ANY new feature, modifying ANY service method, or fixing ANY bug:

1. **STOP** - Do NOT write production code first
2. **READ** - Review business rules in feature CLAUDE.md
3. **WRITE TESTS FIRST** - Write failing tests for all scenarios
4. **RUN** - Verify tests fail (Red)
5. **IMPLEMENT** - Write minimum code to pass tests (Green)
6. **REFACTOR** - Clean up while keeping tests green
7. **DOCUMENT** - Update CLAUDE.md with new invariants

**If you catch yourself writing production code before tests, STOP IMMEDIATELY and write the tests first.**

### Framework & Libraries
- xUnit with `[Fact]` and `[Theory]`/`[InlineData]`
- Moq + AutoMocker (`autoMocker.CreateInstance<T>()`)
- AutoFixture with customizations for DateOnly and recursive types
- FluentAssertions for assertions (`result.Should().Be(expected)`)
- EF Core InMemory with unique database per test (`Guid.NewGuid().ToString()`)

### Test Naming & Structure
- **Naming**: `MethodName_ScenarioCondition_ExpectedResult`
- **Structure**: Strict AAA (Arrange-Act-Assert) with blank line separators
- **Pattern**: One logical assertion per test (FluentAssertions `.And` chaining acceptable)

### What to Mock
- **Always mock**: Repositories (`I*Repository`), external HTTP services, `IConfiguration`
- **Never mock**: Calculators, validators, mappers (pure functions — test directly)

### Coverage Expectations (MUST COVER ALL)
- Every service method: happy path + primary failure path
- Every business rule invariant (documented in feature CLAUDE.md): at least one test
- Every threshold boundary (e.g., >20%, >0.5%): tested at, below, and above
- Analyzers: all boundaries verified
- Calculators: FIFO lots, splits, dividends, partial sells — dedicated test cases
- **New/Modified code**: 100% of new business logic paths tested BEFORE implementation

### Mandatory TDD Scenarios

For **EVERY** feature implementation, you MUST write tests for:

1. **Happy Path**: Expected successful execution
2. **Edge Cases**: Boundary conditions, empty inputs, null values
3. **Error Cases**: All possible exceptions and validation failures
4. **State Changes**: Before/after assertions for mutations
5. **Integration Points**: All external dependencies mocked and verified

### TDD Workflow (STRICT)

```
┌─────────────────────────────────────┐
│  1. Identify Business Requirements  │
│     (Read feature CLAUDE.md)        │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│  2. Write Test Class & Test Cases   │
│     (Cover ALL scenarios)           │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│  3. Run Tests → MUST FAIL (Red)     │
│     (Verify tests are valid)        │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│  4. Implement Production Code       │
│     (Minimum to pass tests)         │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│  5. Run Tests → MUST PASS (Green)   │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│  6. Refactor (Keep tests green)     │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│  7. Update CLAUDE.md Documentation  │
└─────────────────────────────────────┘
```

### Testing Details
- **Controller tests**: Mock `ControllerContext` with `ClaimsPrincipal` for auth scenarios
- **GUID generation**: Use `Guid.NewGuid()`, never `fixture.Create<Guid>()`
- **Test project**: `Babylon.Alfred.Api.Tests` mirrors API folder structure
- **Full testing contract**: See `Babylon.Alfred.Api.Tests/Claude.MD`

### TDD Violations

**NEVER do these:**
- ❌ Write production code before writing tests
- ❌ Skip edge cases or error scenarios
- ❌ Write tests after implementation (that's not TDD)
- ❌ Modify production code without corresponding test changes
- ❌ Commit code with failing tests

**ALWAYS do these:**
- ✅ Write tests first for every new method
- ✅ Cover all business rule invariants
- ✅ Test error paths and exceptions
- ✅ Run tests before committing
- ✅ Keep tests green during refactoring

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

## Context Loading Guide

This guide helps AI agents select the minimal set of CLAUDE.md files needed for a given task:

### Modifying a Service
**Load**: Root + feature CLAUDE.md + `Shared/Repositories/CLAUDE.md`

### Adding a New Feature
**Load**: Root + `Babylon.Alfred.Api/CLAUDE.md` + `Babylon.Alfred.Api/Features/Startup/CLAUDE.md` + `Shared/Data/CLAUDE.md`

### Writing Tests
**Load**: Root (Testing Contract) + `Babylon.Alfred.Api.Tests/Claude.MD` + target feature CLAUDE.md

### Working with Database/Migrations
**Load**: Root + `Shared/Data/CLAUDE.md` + feature CLAUDE.md (if entity belongs to specific feature)

### Working with Authentication
**Load**: Root + `Features/Authentication/CLAUDE.md`

### Working with Background Jobs
**Load**: Root + `Babylon.Alfred.Worker/CLAUDE.md` + related feature CLAUDE.md

### Working with External Integrations
**Load**: Root + `Infrastructure/CLAUDE.md` + consuming feature CLAUDE.md

## Global Rules

### 🚨 RULE #0: TEST-DRIVEN DEVELOPMENT IS MANDATORY 🚨

**BEFORE ANY CODE CHANGE:**
- ✋ Write the test FIRST
- 🔴 Verify it FAILS (Red)
- ✅ Implement to make it PASS (Green)
- ♻️ Refactor while keeping it GREEN

**This is not optional. This is not a suggestion. This is a requirement.**

If an AI agent or developer writes production code without writing tests first, they have violated the project's core development methodology.

### ✅ DOs
- One controller per aggregate (thin, delegate all logic to services)
- Primary constructor injection everywhere (C# 12)
- Return `ApiResponse<T>` for all success responses
- Use `LoggerExtensions` methods (never raw `logger.LogX()`)
- All repository methods: async, `Async` suffix
- Use `User.GetUserId()` to extract the authenticated user ID
- Register everything as Scoped unless there's a documented reason not to

### ❌ DON'Ts
- No business logic in controllers
- No business logic in repositories
- No repository-to-repository calls
- No `Async` suffix on service method names
- No direct `DbContext` use outside of repositories
- No `Controller` base class (use `ControllerBase`)
- No `DateTime.Now` — always `DateTime.UtcNow`
