# Architecture — Babylon Alfred API

## Business Context

Babylon is a personal investment portfolio API. Named after Batman's butler Alfred, it tracks transactions across brokers, computes FIFO positions and P&L, and (once wired up) tells the user what to buy or sell to hit target allocations. Backend only — clients are AI agents via MCP (planned) and any future frontend.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Language | C# 12, .NET 9.0 |
| Web Framework | ASP.NET Core |
| ORM | Entity Framework Core 8.0 |
| Database | PostgreSQL 17 on AWS RDS (Npgsql) |
| Logging | Serilog (structured, Console + File sinks) |
| API Docs | Swagger / Swashbuckle |
| Auth | JWT Bearer + Google OAuth + BCrypt |
| Scheduling | Quartz.NET |
| External Data | Yahoo Finance API |
| Testing | xUnit, Moq, Moq.AutoMock, AutoFixture, FluentAssertions, EF Core InMemory |
| IaC | Terraform (AWS VPC, RDS, Secrets Manager) |
| Deploy | Fly.io (API + Worker containers), region: CDG (Paris) |
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
2. `UseStaticFiles()` — serves `wwwroot/` (architecture diagram)
3. `RequestLoggingMiddleware` — logs all requests with timing
4. `GlobalErrorHandlerMiddleware` — catches unhandled exceptions, returns `ApiErrorResponse`
5. Swagger UI at `/swagger`
6. `UseAuthorization()`
7. `MapControllers()`

---

## DI Conventions

- **Default lifetime**: Scoped (per-request) for all services and repositories
- **Primary constructor injection**: C# 12 syntax everywhere
- **Feature registration pattern**: Each feature registers via an extension method wired into `Features/Startup/Extensions/ServiceCollectionExtensions.RegisterFeatures()`
- Current registration: `RegisterInvestmentServices()`, then Authentication services inline

---

## Controller Conventions

- Inherit from `BabylonControllerBase`
- `[ApiController]` attribute
- `[Authorize]` on all user-data endpoints (Securities currently has none — see investments.md)
- Extract user ID: `User.GetUserId()` (reads `Sub` claim from JWT)
- Return via base helpers: `Success(data)`, `Created(data)`, `Fail(error, statusCode)`
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

- **API**: Fly.io (`fly.api.toml`), port 8080, HTTPS enforced, health check at `GET /health`
- **Worker**: Fly.io (`fly.worker.toml`)
- **Database**: AWS RDS PostgreSQL (`iac/`), treated as a development database until the planned Fly Postgres migration (`fly.db.prod.toml`)
- **Docker**: Multi-stage build. Base: `mcr.microsoft.com/dotnet/aspnet:9.0`
- **CI/CD**: GitHub Actions on push/PR to `main`. Build + test in Release mode.

---

## API Endpoints Reference

| Feature | Base Route | Methods |
|---------|-----------|---------|
| Health | `/health` | GET (public) |
| Auth | `/api/v1/auth` | POST google, login, register, refresh, logout |
| Portfolios | `/api/v1/portfolios` | GET |
| Portfolio history | `/api/v1/portfolios/history` | GET, GET latest |
| Transactions | `/api/v1/transactions` | POST, POST bulk, GET, PUT, DELETE |
| Securities | `/api/v1/securities` | GET, GET by ticker, POST, POST admin, PUT, DELETE |
| Cash | `/api/v1/cash` | PUT |
| Allocations | `/api/v1/allocations` | GET, PUT |

---

## Adding a New Feature — Checklist

- [ ] Create `Features/{FeatureName}/` with: `Controllers/`, `Services/`, `Models/Requests/`, `Models/Responses/`, `Extensions/`
- [ ] Implement `Extensions/ServiceCollectionExtensions.cs` with a `Register{FeatureName}Services()` method
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
- `fly.worker.toml` — Fly.io Worker deployment config
- `.github/workflows/` — CI/CD pipelines
- `fly.api.toml` — Fly.io API deployment config
- `test-api.http` — HTTP request samples
