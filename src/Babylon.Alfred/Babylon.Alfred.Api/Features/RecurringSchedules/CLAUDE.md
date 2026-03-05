# RecurringSchedules Feature

## Product Overview

Manages recurring investment plans that users set up for automated/scheduled purchases of securities. Tracks which securities a user wants to invest in regularly, on which platform, and the target amount.

## Business Requirements

### Recurring Schedules
- Users create recurring investment plans tied to a specific security.
- Each schedule tracks: UserId, SecurityId, Platform (e.g., "Trading212", "IBKR"), TargetAmount, IsActive.
- Only active schedules are retrieved for a user.
- **Upsert semantics**: Unique key is `(UserId, SecurityId)`. If exists, update; else create.
- Schedules can be deleted by ID.

### Use Cases
- Track regular investment contributions across different brokerage platforms.
- Inform rebalancing calculations about planned future investments.
- Integrate with allocation strategies (which securities are enabled for weekly/bi-weekly/monthly).

## Architecture

| Component | Purpose |
|-----------|---------|
| **RecurringSchedulesController** | GET active, POST create/update, DELETE |
| **RecurringScheduleService** | CreateOrUpdateAsync, GetActiveByUserIdAsync, DeleteAsync |
| **Models** | CreateRecurringScheduleRequest, RecurringScheduleDto |

## Dependencies

- `IRecurringScheduleRepository` - Data access for recurring schedules
- `ISecurityRepository` - Resolves security metadata for response DTOs

## Domain Entity

**RecurringSchedule**: Id, UserId, SecurityId, Platform?, TargetAmount?, IsActive, CreatedAt

**Table**: `recurring_schedules`

## Test Strategy

Service tests must cover:
- Upsert idempotency: creating same (UserId, SecurityId) twice → updates existing
- Deleting non-existent schedule → throws or returns error
