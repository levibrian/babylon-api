# RecurringSchedules Feature

## Product Overview

Manages recurring investment plans that users set up for automated/scheduled purchases of securities. Tracks which securities a user wants to invest in regularly, on which platform, and the target amount.

## Business Requirements

### Recurring Schedules
- Users create recurring investment plans tied to a specific security.
- Each schedule tracks: UserId, SecurityId, Platform (e.g., "Trading212", "IBKR"), TargetAmount, IsActive.
- Only active schedules are retrieved for a user.
- Schedules support create-or-update semantics (upsert).
- Schedules can be deleted by ID.

### Use Cases
- Track regular investment contributions across different brokerage platforms.
- Inform rebalancing calculations about planned future investments.
- Integrate with allocation strategies (which securities are enabled for weekly/bi-weekly/monthly).

## Architecture

```
Features/RecurringSchedules/
├── Controllers/
│   └── RecurringSchedulesController.cs   # GET active, POST create/update, DELETE
├── Services/
│   ├── IRecurringScheduleService.cs      # CreateOrUpdateAsync, GetActiveByUserIdAsync, DeleteAsync
│   └── RecurringScheduleService.cs       # Business logic
├── Models/
│   ├── Requests/
│   │   └── CreateRecurringScheduleRequest.cs  # { SecurityId, Platform?, TargetAmount?, IsActive }
│   └── Responses/
│       └── RecurringScheduleDto.cs            # { Id, SecurityId, Ticker, SecurityName, Platform, TargetAmount, IsActive }
└── Extensions/
    └── ServiceCollectionExtensions.cs    # DI registration
```

## Dependencies

- `IRecurringScheduleRepository` - Data access for recurring schedules
- `ISecurityRepository` - Resolves security metadata for response DTOs

## Domain Entity

```csharp
RecurringSchedule: Id, UserId, SecurityId, Platform?, TargetAmount?, IsActive, CreatedAt
```

Table: `recurring_schedules`
