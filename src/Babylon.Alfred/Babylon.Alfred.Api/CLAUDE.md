# Babylon.Alfred.Api - API Project Context

## Overview

The main ASP.NET Core REST API for the Babylon investment platform. Serves the React frontend and exposes all portfolio management, authentication, analytics, and rebalancing endpoints.

## Project Structure

```
Babylon.Alfred.Api/
├── Program.cs                          # Startup, DI, middleware pipeline
├── Constants.cs                        # Solution-wide constants (RootUserId)
├── appsettings.json                    # Configuration (JWT, CORS, DB, Rebalancing, Gemini)
├── Dockerfile                          # Multi-stage Docker build
│
├── Features/                           # Vertical slices (self-contained feature modules)
│   ├── Authentication/                 # User auth (JWT, Google OAuth, refresh tokens)
│   ├── Investments/                    # Core feature: portfolios, transactions, analytics
│   ├── RecurringSchedules/             # Recurring investment plans
│   ├── Telegram/                       # Telegram bot integration (in progress)
│   ├── Startup/                        # Health check, startup DI registration
│   └── TestFeature/                    # Development test endpoint
│
├── Infrastructure/                     # External service integrations
│   └── YahooFinance/                   # Market data provider
│
└── Shared/                             # Cross-cutting concerns
    ├── Data/                           # DbContext, entity models, EF configurations, migrations
    ├── Repositories/                   # Repository interfaces + implementations
    ├── Middlewares/                     # Request logging, global error handler
    ├── Logging/                        # LoggerExtensions for structured logging
    ├── Models/                         # Shared DTOs (ApiResponse, ApiErrorResponse)
    └── Extensions/                     # Claims extensions (User.GetUserId())
```

## Startup (Program.cs)

Configuration order:
1. Serilog from `appsettings.json`
2. Controllers + Newtonsoft.Json (StringEnumConverter, UnixDateTimeConverter)
3. Swagger
4. CORS (configurable origins + defaults)
5. PostgreSQL DbContext with retry on failure (3 retries, 5s delay)
6. Feature registration via `builder.Services.RegisterFeatures()`
7. JWT authentication (conditional on SecretKey presence)

Middleware pipeline:
1. `UseCors()` (must be first for preflight)
2. `RequestLoggingMiddleware`
3. `GlobalErrorHandlerMiddleware`
4. Swagger UI at `/swagger`
5. `UseAuthorization()`
6. `MapControllers()`

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

Each feature registers its own services via `ServiceCollectionExtensions.cs`:
- `Features/Startup/Extensions/ServiceCollectionExtensions.cs` is the root registrar that calls feature-specific registrations.
- Services registered as **Scoped** (per-request lifetime).
- Repositories registered as **Scoped**.

## Configuration (appsettings.json)

Key sections:
- `ConnectionStrings:DefaultConnection` - PostgreSQL connection
- `Authentication:Jwt` - SecretKey, Issuer (`BabylonAlfredApi`), Audience (`BabylonAlfredClient`), ExpirationMinutes (1440)
- `Authentication:Google:ClientId` - Google OAuth client ID
- `Cors:AllowedOrigins` - Additional allowed origins
- `Rebalancing:TimedActions` - Buy/sell percentile thresholds, noise threshold
- `Rebalancing:Gemini` - AI rebalancing config (Enabled, ApiKey, Model, Temperature)
- `Serilog` - Logging levels and sinks

## Adding a New Feature

1. Create a folder under `Features/{FeatureName}/`
2. Add subfolders: `Controllers/`, `Services/`, `Models/Requests/`, `Models/Responses/`
3. Create `Extensions/ServiceCollectionExtensions.cs` to register DI
4. Wire up in `Features/Startup/Extensions/ServiceCollectionExtensions.cs`
5. Add corresponding test folder under `Babylon.Alfred.Api.Tests/Features/{FeatureName}/`
6. Create a `CLAUDE.md` inside the feature folder documenting business requirements

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
