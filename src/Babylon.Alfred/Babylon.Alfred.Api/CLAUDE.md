# Babylon.Alfred.Api - API Project Context

## Overview

The main ASP.NET Core REST API for the Babylon investment platform. Serves the React frontend and exposes all portfolio management, authentication, analytics, and rebalancing endpoints.

## Project Structure

**Follows root architectural principles**: Vertical slices under `Features/`, cross-cutting in `Shared/`, external adapters in `Infrastructure/`.

### Feature Modules
- **Authentication**: JWT, Google OAuth, refresh tokens
- **Investments**: Portfolios, transactions, securities, analytics, rebalancing (largest feature, see `Features/Investments/CLAUDE.md`)
- **RecurringSchedules**: Recurring investment plans
- **Telegram**: Bot integration (in progress, minimal implementation)
- **Startup**: Health checks, root DI registration

### Shared Layer (Cross-Cutting)
- **Data**: `BabylonDbContext`, entity models, EF configurations, migrations (see `Shared/Data/CLAUDE.md`)
- **Repositories**: Repository pattern implementations (see `Shared/Repositories/CLAUDE.md`)
- **Middlewares**: Request logging, global error handler
- **Logging**: `LoggerExtensions` for structured logging
- **Models**: Shared DTOs (`ApiResponse<T>`, `ApiErrorResponse`)
- **Extensions**: Claims extensions (`User.GetUserId()`)

### Infrastructure Layer
- **YahooFinance**: Market data provider (see `Infrastructure/CLAUDE.md`)

### Root Files
- `Program.cs` - Startup, DI, middleware pipeline
- `Constants.cs` - Solution-wide constants (RootUserId)
- `appsettings.json` - Configuration (JWT, CORS, DB, Rebalancing, Gemini)
- `Dockerfile` - Multi-stage Docker build

## Startup (Program.cs)

Configuration order:
1. Serilog from `appsettings.json`
2. Controllers + Newtonsoft.Json (StringEnumConverter, UnixDateTimeConverter)
3. Swagger
4. CORS (configurable origins + defaults)
5. PostgreSQL DbContext with retry on failure (3 retries, 5s delay)
6. Feature registration via `builder.Services.RegisterFeatures()`
7. JWT authentication (conditional on SecretKey presence)

Middleware pipeline (order matters):
1. `UseCors()` — **Must be first** to handle preflight OPTIONS requests before any other middleware
2. `RequestLoggingMiddleware` — Logs all requests early, captures timing for full pipeline
3. `GlobalErrorHandlerMiddleware` — Catches unhandled exceptions from downstream middleware/controllers
4. Swagger UI at `/swagger` — Serves API documentation
5. `UseAuthorization()` — Enforces `[Authorize]` attributes on controllers
6. `MapControllers()` — Routes requests to controller actions

## API Versioning

All endpoints are under `/api/v1/`. Route prefix is set per-controller via `[Route("api/v1/[resource]")]`.

## API Endpoints

| Feature | Base Route | Methods |
|---------|-----------|---------|
| Health | `/health` | GET |
| Auth | `/api/v1/auth` | POST google, login, register, refresh, logout |
| Portfolios | `/api/v1/portfolios` | GET (by userId) |
| Transactions | `/api/v1/transactions` | POST, POST bulk, GET, PUT, DELETE |
| Securities | `/api/v1/securities` | GET, GET by ticker, POST, POST search-and-create |
| Allocations | `/api/v1/allocations` | GET (by userId), POST (set strategy) |
| Analytics | `/api/v1/analytics` | GET (by userId) |
| Insights | `/api/v1/insights` | GET (by userId) |
| Rebalancing | `/api/v1/rebalancing` | POST suggestions, POST apply-smart-rebalancing |
| History | `/api/v1/history` | GET (by userId) |
| Market | `/api/v1/market` | GET prices |
| Cash | `/api/v1/cash` | GET (by userId), PUT (update) |
| User | `/api/v1/user` | User profile endpoints |
| Recurring | `/api/v1/recurring-schedules` | GET, POST, DELETE |

## Dependency Injection

**DI rules**: See root `CLAUDE.md` § Global Rules. Default lifetime is **Scoped** (per-request).

Each feature registers its own services via `ServiceCollectionExtensions.cs`:
- `Features/Startup/Extensions/ServiceCollectionExtensions.cs` is the root registrar that calls feature-specific registrations.
- Features use extension method pattern: `builder.Services.AddAuthenticationFeature()`, `builder.Services.AddInvestmentsFeature()`, etc.

## Configuration (appsettings.json)

See `appsettings.json` directly. Key sections: `ConnectionStrings`, `Authentication` (JWT, Google), `Cors`, `Rebalancing` (TimedActions, Gemini), `Serilog`.

## Adding a New Feature Checklist

- [ ] Create `Features/{FeatureName}/` with subfolders: `Controllers/`, `Services/`, `Models/Requests/`, `Models/Responses/`, `Extensions/`
- [ ] Implement `Extensions/ServiceCollectionExtensions.cs` with `Add{FeatureName}Feature()` method
- [ ] Wire into `Features/Startup/Extensions/ServiceCollectionExtensions.cs`
- [ ] Add test folder: `Babylon.Alfred.Api.Tests/Features/{FeatureName}/` (mirror structure)
- [ ] Create `Features/{FeatureName}/CLAUDE.md` documenting business rules and invariants
- [ ] Add route entries to § API Endpoints table in this file

## Controller Conventions

- Inherit from `ControllerBase` (not `Controller` - no view support needed).
- Use `[ApiController]` attribute.
- Use `[Authorize]` on protected endpoints.
- Extract userId via `User.GetUserId()` extension method (reads `Sub` claim).
- Return `Ok(new ApiResponse<T> { Success = true, Data = result })` for success.
- Return `NotFound()`, `BadRequest()` for errors. Unhandled exceptions are caught by middleware.

## Service Conventions

- Interface + implementation pattern (`IFooService` / `FooService`).
- Primary constructor injection (C# 12 syntax).
- No `Async` suffix on method names.
- Validate inputs, throw exceptions for invalid state.
- Use `ILogger<T>` with `LoggerExtensions` for structured logging.

## Error Handling

- `GlobalErrorHandlerMiddleware` catches all unhandled exceptions.
- Extracts userId from JWT claims for context.
- Returns standardized `ApiErrorResponse` with 500 status.
- Logs exception with type, message, HTTP method, path, and userId.
