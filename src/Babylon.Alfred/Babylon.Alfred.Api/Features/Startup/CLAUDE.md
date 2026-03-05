# Startup Feature

## Overview

The Startup feature handles application bootstrapping concerns: health checks and root-level feature registration. This is the orchestration layer that wires together all other features.

## Components

| Component | Purpose |
|-----------|---------|
| **HealthController** | GET `/health` endpoint for deployment health checks |
| **ServiceCollectionExtensions.RegisterFeatures()** | Root DI registration method that calls all feature-specific registration methods |

## Feature Registration Order

`RegisterFeatures()` calls feature registrations in this order:

1. `RegisterTelegram()` - Telegram bot client (no dependencies)
2. `RegisterInvestmentServices()` - Core investment services (no dependencies on other features)
3. `RegisterRecurringScheduleServices()` - Recurring schedules (no dependencies on other features)
4. Authentication services (inline registration, no dependencies)

**Note**: Order currently doesn't matter as features are independent. If future cross-feature dependencies arise, order MUST be documented here.

## Health Check Endpoint

**Route**: `GET /health`

**Purpose**: Used by deployment orchestrators (Fly.io) to verify app is running.

**Returns**: `200 OK` with `{ "status": "Healthy" }`

**No authentication required** (public endpoint).

## DI Registration Pattern

Each feature exposes a `Register{FeatureName}()` extension method:

```csharp
// Example pattern
public static void RegisterInvestmentServices(this IServiceCollection services)
{
    services.AddScoped<IPortfolioService, PortfolioService>();
    services.AddScoped<ITransactionService, TransactionService>();
    // ... more registrations
}
```

Authentication is an exception - currently registered inline in `RegisterFeatures()` rather than via its own extension method.

## Adding a New Feature Registration

When adding a new feature:

1. Create `{Feature}/Extensions/ServiceCollectionExtensions.cs` with `Register{Feature}()` method
2. Add call to `RegisterFeatures()` in this file
3. If feature depends on another feature's services, register dependent feature AFTER its dependencies
4. Document any ordering constraints in § Feature Registration Order above

## Invariants

- **Health endpoint must remain public** (no `[Authorize]`) - used by infrastructure for liveness probes
- **Feature registration order matters only if cross-dependencies exist** - document any new dependencies immediately
