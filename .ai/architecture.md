# Architecture — Babylon Alfred API

## Business Context

Babylon is a personal investment portfolio management platform. Named after Batman's butler Alfred, it serves as an automated assistant that tracks stock portfolio transactions, manages positions, calculates portfolio metrics, and provides rebalancing recommendations. Frontend is an Angular application deployed at `babylonfinance.vercel.app`.

---

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

---

## Solution Structure

```
Babylon.Alfred.Api/          ← REST API (main project)
├── Features/                ← Vertical slices (self-contained)
├── Shared/                  ← Cross-cutting concerns
│   ├── Data/                ← DbContext, entities, migrations
│   ├── Repositories/        ← Repository pattern
│   ├── Middlewares/         ← Request logging, error handler
│   ├── Logging/             ← LoggerExtensions
│   ├── Models/              ← ApiResponse<T>, ApiErrorResponse
│   └── Extensions/          ← Claims helpers (User.GetUserId())
└── Infrastructure/          ← External adapters (Yahoo Finance)

Babylon.Alfred.Worker/       ← Background jobs (Quartz.NET)
Babylon.Alfred.Api.Tests/    ← xUnit test project (mirrors API structure)
```

### Projects
- **Babylon.Alfred.Api**: REST API. Vertical slice architecture.
- **Babylon.Alfred.Worker**: Background job service. References API project for models, repos, calculators.
- **Babylon.Alfred.Api.Tests**: xUnit tests. Mirrors API folder structure exactly.

---

## Vertical Slice Architecture

Each feature is self-contained under `Features/{FeatureName}/`:

```
Features/{FeatureName}/
├── Controllers/
├── Services/
├── Models/
│   ├── Requests/
│   └── Responses/
└── Extensions/
    └── ServiceCollectionExtensions.cs   ← Add{FeatureName}Feature()
```

**Rule**: Features do NOT depend on each other. Cross-cutting utilities only go in `Shared/`.

### Layering Within Features

```
Controller → Service → Repository → DbContext
                 |
                 ↓
         Calculator / Validator / Mapper (pure, no dependencies)
```

---

## Request/Response Envelope

All endpoints return a standard envelope:

- **Success**: `ApiResponse<T>` → `{ success: true, data: T }`
- **Error**: `ApiErrorResponse` → `{ success: false, message: string, errors: [] }`

---

## Middleware Pipeline (order matters)

1. `UseCors()` — **must be first** to handle preflight OPTIONS
2. `RequestLoggingMiddleware` — logs all requests with timing
3. `GlobalErrorHandlerMiddleware` — catches unhandled exceptions, returns `ApiErrorResponse`
4. Swagger UI at `/swagger`
5. `UseAuthorization()`
6. `MapControllers()`

---

## DI Conventions

- **Default lifetime**: Scoped (per-request) for all services and repositories
- **Primary constructor injection**: C# 12 syntax everywhere
- **Feature registration pattern**: Each feature has `Add{FeatureName}Feature()` or `Register{FeatureName}()` wired into `Features/Startup/Extensions/ServiceCollectionExtensions.RegisterFeatures()`
- Current registration order: Telegram → Investments → RecurringSchedules → Authentication

---

## Controller Conventions

- Inherit from `ControllerBase` (never `Controller`)
- `[ApiController]` attribute
- `[Authorize]` on all protected endpoints
- Extract user ID: `User.GetUserId()` (reads `Sub` claim from JWT)
- Return: `Ok(new ApiResponse<T> { Success = true, Data = result })`
- Zero business logic — delegate everything to service

---

## Service Conventions

- Interface + implementation: `IFooService` / `FooService`
- Primary constructor injection
- **No `Async` suffix** on method names (opposite of repositories)
- Validate inputs, throw exceptions for invalid state
- Use `ILogger<T>` with `LoggerExtensions` methods (never raw `logger.LogX()`)
- Never access `DbContext` directly

---

## Repository Conventions

- Interface + implementation: `I{Entity}Repository` / `{Entity}Repository`
- **Always use `Async` suffix** on all methods
- All methods return `Task<T>`
- No business logic — data access only
- No repository-to-repository calls
- Eager load navigation properties via `.Include()` where needed

---

## Code Conventions

- **Nullable reference types**: Enabled solution-wide
- **Braces**: Allman style (opening brace on new line)
- **DateTime**: Always `DateTime.UtcNow` — never `DateTime.Now`
- **Enums**: Serialized as strings via `StringEnumConverter`
- **Indentation**: 4 spaces for C#, 2 spaces for JSON/YAML
- **Line endings**: CRLF
- **API versioning**: All endpoints under `/api/v1/`

---

## Logging (Serilog)

Use `LoggerExtensions` extension methods — never raw `logger.LogX()`:

| Method | Level | Purpose |
|--------|-------|---------|
| `LogOperationStart` | Info | Entry into a method/operation |
| `LogOperationSuccess` | Info | Successful completion |
| `LogDatabaseOperation` | Info | Database CRUD with entity type + count |
| `LogApiRequest` | Info | HTTP request with method, path, userId |
| `LogApiResponse` | Varies | 5xx=Error, 4xx=Warning, 2xx=Info |
| `LogPerformance` | Varies | >1000ms=Warning, else Info |
| `LogValidationFailure` | Warning | Validation errors |
| `LogBusinessRuleViolation` | Warning | Business rule violations |

---

## Authentication (Summary)

- **JWT**: HS256, 24h expiry, zero clock skew. Claims: Sub (userId), Email, UniqueName, AuthProvider.
- **Refresh tokens**: 7-day, single-use, revoked on reuse or new login.
- **Google OAuth**: Validates IdToken via `GoogleJsonWebSignature.ValidateAsync()`.
- **BCrypt**: Work factor 11 for password hashing.
- **One email = one account** (unified auth — see `.ai/features/authentication.md` for full flows).

---

## Deployment

- **API**: Fly.io, 1 shared CPU, 512MB RAM, port 8080, HTTPS enforced, health check at `GET /health`
- **Database**: Fly.io managed PostgreSQL, always-on
- **Docker**: Multi-stage build. Base: `mcr.microsoft.com/dotnet/aspnet:9.0`
- **CI/CD**: GitHub Actions on push/PR to `main`. Build + test in Release mode.

---

## API Endpoints Reference

| Feature | Base Route | Methods |
|---------|-----------|---------|
| Health | `/health` | GET (public) |
| Auth | `/api/v1/auth` | POST google, login, register, refresh, logout |
| Portfolios | `/api/v1/portfolios` | GET |
| Transactions | `/api/v1/transactions` | POST, POST bulk, GET, PUT, DELETE |
| Securities | `/api/v1/securities` | GET, GET by ticker, POST, POST search-and-create |
| Allocations | `/api/v1/allocations` | GET, POST |
| Analytics | `/api/v1/analytics` | GET |
| Insights | `/api/v1/insights` | GET |
| Rebalancing | `/api/v1/rebalancing` | POST suggestions, POST apply-smart-rebalancing |
| History | `/api/v1/history` | GET |
| Market | `/api/v1/market` | GET prices |
| Cash | `/api/v1/cash` | GET, PUT |
| User | `/api/v1/user` | User profile endpoints |
| Recurring | `/api/v1/recurring-schedules` | GET, POST, DELETE |

---

## Adding a New Feature — Checklist

- [ ] Create `Features/{FeatureName}/` with: `Controllers/`, `Services/`, `Models/Requests/`, `Models/Responses/`, `Extensions/`
- [ ] Implement `Extensions/ServiceCollectionExtensions.cs` with `Add{FeatureName}Feature()`
- [ ] Wire into `Features/Startup/Extensions/ServiceCollectionExtensions.RegisterFeatures()`
- [ ] Add test folder: `Babylon.Alfred.Api.Tests/Features/{FeatureName}/`
- [ ] Create `.ai/features/{feature-name}.md` documenting business rules and invariants
- [ ] Add route entries to API Endpoints Reference table above

---

## Shared Layer — Anti-Pattern

**DO NOT** add feature-specific logic to `Shared/`. Only generic, reusable utilities belong there.

```csharp
// BAD — belongs in Features/Investments/
public class PortfolioHelper { public decimal CalculateRebalancingThreshold() { ... } }

// GOOD — generic, reusable
public static class ClaimsExtensions { public static Guid GetUserId(this ClaimsPrincipal user) { ... } }
```

---

## Key Anchor Files

- `iac/` — Terraform infrastructure (AWS VPC, RDS, Secrets)
- `.github/workflows/` — CI/CD pipelines
- `fly.api.toml` — Fly.io API deployment config
- `test-api.http` — HTTP request samples
