# Shared Layer

## Overview

Cross-cutting concerns shared by all features. Contains: Data, Repositories, Middlewares, Logging, Models, Extensions.

**Rule**: Do not add feature-specific logic here. Shared components are generic, reusable utilities.

## Layer Contents

| Folder | Purpose | Details |
|--------|---------|---------|
| **Data** | DbContext, entity models, EF configurations, migrations | See `Shared/Data/CLAUDE.md` |
| **Repositories** | Repository pattern implementations (data access) | See `Shared/Repositories/CLAUDE.md` |
| **Logging** | Structured logging extensions (`LoggerExtensions`) | See `Shared/Logging/CLAUDE.md` |
| **Middlewares** | Request logging, global error handler | HTTP pipeline middleware |
| **Models** | Shared DTOs (`ApiResponse<T>`, `ApiErrorResponse`) | API response envelopes |
| **Extensions** | Claims extensions (`User.GetUserId()`) | Helper extension methods |

## Context Loading Shortcut

When working on **anything in Shared**, load:
- Root `CLAUDE.md` (Global Rules)
- Specific layer CLAUDE.md (e.g., `Shared/Data/CLAUDE.md` for DB work)
- Feature CLAUDE.md only if feature-specific context needed

## Anti-Pattern Warning

❌ **DO NOT** add feature-specific business logic to Shared:

```csharp
// BAD: This belongs in Features/Investments/
public class PortfolioHelper
{
    public decimal CalculateRebalancingThreshold() { ... }
}
```

✅ **DO** add generic utilities:

```csharp
// GOOD: Generic, reusable across all features
public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) { ... }
}
```

## DI Registration

Shared layer components are registered in:
- `Program.cs` (DbContext, Repositories)
- Not in feature-specific `ServiceCollectionExtensions`

## Testing

Shared components are tested in:
- `Babylon.Alfred.Api.Tests/Shared/` (mirrors structure)
- Repository tests use EF Core InMemory
- Extension method tests use standard xUnit patterns
