# Recurring Schedules Feature

## Overview

Manages recurring investment plans that users set up for automated/scheduled purchases of securities.

---

## Business Rules

- Each schedule tracks: `UserId`, `SecurityId`, `Platform` (e.g., "Trading212", "IBKR"), `TargetAmount`, `IsActive`
- Only **active** schedules are returned for a user
- **Upsert semantics**: Unique key is `(UserId, SecurityId)`. If exists → update. If not → create.
- Schedules can be deleted by ID.

---

## Components

| Component | Purpose |
|-----------|---------|
| `RecurringSchedulesController` | GET active, POST create/update, DELETE |
| `RecurringScheduleService` | `CreateOrUpdateAsync`, `GetActiveByUserIdAsync`, `DeleteAsync` |
| `IRecurringScheduleRepository` | Data access |
| `ISecurityRepository` | Resolves security metadata for response DTOs |

---

## Domain Entity

**RecurringSchedule**: `Id`, `UserId`, `SecurityId`, `Platform?`, `TargetAmount?`, `IsActive`, `CreatedAt`

**Table**: `recurring_schedules`

---

## Use Cases

- Track regular investment contributions across different brokerage platforms
- Inform rebalancing calculations about planned future investments
- Integrate with allocation strategies (weekly/bi-weekly/monthly enablement)

---

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/recurring-schedules` | Get active schedules for authenticated user |
| POST | `/api/v1/recurring-schedules` | Create or update a schedule (upsert) |
| DELETE | `/api/v1/recurring-schedules/{id}` | Delete a schedule |

---

## Test Anchors

- Upsert idempotency: creating same `(UserId, SecurityId)` twice → updates existing, does not duplicate
- Deleting non-existent schedule → throws or returns error
- Only active schedules returned in GET
