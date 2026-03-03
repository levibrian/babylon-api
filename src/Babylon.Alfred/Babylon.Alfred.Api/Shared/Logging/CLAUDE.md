# Shared/Logging - Logging Layer

## Overview

Provides structured logging extensions and conventions for the entire API. Built on Serilog with consistent patterns for operation tracking, performance monitoring, and error context.

## Structure

```
Shared/Logging/
└── LoggerExtensions.cs    # Extension methods for ILogger<T>
```

## LoggerExtensions Methods

| Method | Level | Purpose |
|--------|-------|---------|
| `LogOperationStart` | Information | Entry into a method/operation with optional context |
| `LogOperationSuccess` | Information | Successful completion with optional result summary |
| `LogDatabaseOperation` | Information | Database CRUD with entity type, identifier, and optional count |
| `LogApiRequest` | Information | HTTP request with method, path, and userId |
| `LogApiResponse` | Varies | HTTP response. 5xx=Error, 4xx=Warning, 2xx=Information |
| `LogPerformance` | Varies | Operation timing. >1000ms=Warning, else Information |
| `LogValidationFailure` | Warning | Validation errors with operation context |
| `LogBusinessRuleViolation` | Warning | Business rule violations with rule description |

## Serilog Configuration (appsettings.json)

- **Console sink**: Structured output with `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}`.
- **Enrichment**: `FromLogContext`, `WithMachineName`, `WithThreadId`.
- **Minimum levels**: Default=Information, Microsoft=Warning, EFCore=Information.

## Middleware Logging

Located in `Shared/Middlewares/`:

### RequestLoggingMiddleware
- Logs every HTTP request with start/end timing via `Stopwatch`.
- Captures method, path, status code, and elapsed milliseconds.

### GlobalErrorHandlerMiddleware
- Catches unhandled exceptions.
- Extracts userId from JWT `Sub` claim for context.
- Logs exception type, message, HTTP method, path, userId.
- Logs inner exceptions separately.
- Returns `ApiErrorResponse` with 500 status.

## Conventions

- Use `LoggerExtensions` methods instead of raw `logger.LogInformation()` calls for consistency.
- Always include operation context (what was being attempted when the log occurred).
- Performance-sensitive operations should use `LogPerformance` to track timing.
- Database operations should use `LogDatabaseOperation` with entity type and count.
