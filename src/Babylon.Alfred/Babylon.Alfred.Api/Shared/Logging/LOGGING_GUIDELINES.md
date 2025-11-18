# Logging Guidelines

## Philosophy
- **Pragmatic**: Log what matters, not everything
- **Structured**: Use structured logging with context
- **Actionable**: Logs should help diagnose issues and understand behavior
- **Performance-aware**: Don't log in tight loops or hot paths unnecessarily

## Log Levels

### Information
- **When**: Normal operations, successful completions, important state changes
- **Examples**: 
  - API requests/responses
  - Database operations (create, update, delete)
  - Successful business operations
  - Job execution start/completion

### Warning
- **When**: Recoverable issues, validation failures, business rule violations
- **Examples**:
  - Validation failures
  - Rate limiting
  - Missing optional data
  - Retryable errors

### Error
- **When**: Exceptions, failures that need attention, unexpected errors
- **Examples**:
  - Exceptions (always log with full context)
  - Failed database operations
  - External API failures
  - Critical business logic failures

### Debug
- **When**: Detailed diagnostic information (typically disabled in production)
- **Examples**:
  - Detailed algorithm steps
  - Complex calculations
  - Internal state dumps

## What to Log

### ✅ DO Log:
1. **API Entry/Exit**: All API requests with method, path, user context
2. **Database Operations**: Create, update, delete operations with entity type
3. **External Calls**: HTTP requests to external APIs (Yahoo Finance, etc.)
4. **Business Operations**: Important business logic operations (transactions, portfolio calculations)
5. **Errors**: All exceptions with full context
6. **Performance**: Operations taking >100ms or configurable threshold
7. **State Changes**: Important state transitions (job started, completed)

### ❌ DON'T Log:
1. **Loop iterations**: Don't log inside tight loops
2. **Sensitive data**: Passwords, tokens, PII (mask if necessary)
3. **Noise**: Don't log "everything is fine" repeatedly
4. **Redundant info**: Don't log the same thing multiple times

## Patterns

### Use Structured Logging
```csharp
// ✅ Good - structured with context
logger.LogInformation("Created transaction {TransactionId} for user {UserId}", transactionId, userId);

// ❌ Bad - string interpolation
logger.LogInformation($"Created transaction {transactionId} for user {userId}");
```

### Use Extension Methods
```csharp
// ✅ Good - consistent pattern
logger.LogOperationStart("CreateTransaction", new { UserId = userId });
logger.LogDatabaseOperation("Create", "Transaction", transactionId);
logger.LogOperationSuccess("CreateTransaction", new { TransactionId = transactionId });
```

### Include Context
```csharp
// ✅ Good - includes relevant context
logger.LogError(ex, "Failed to fetch price for {Ticker} from Yahoo Finance", ticker);

// ❌ Bad - missing context
logger.LogError(ex, "Failed to fetch price");
```

## Performance Considerations

1. **Avoid logging in hot paths** unless necessary
2. **Use appropriate log levels** - Debug logs are disabled in production
3. **Don't serialize large objects** - log identifiers instead
4. **Use scoped logging** where possible for better performance

## Examples

### API Controller
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateRequest request)
{
    _logger.LogApiRequest("POST", "/api/v1/transactions", request.UserId);
    
    try
    {
        var result = await _service.Create(request);
        _logger.LogApiResponse("POST", "/api/v1/transactions", 200, stopwatch.ElapsedMilliseconds);
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create transaction for user {UserId}", request.UserId);
        throw;
    }
}
```

### Repository
```csharp
public async Task<Transaction> Add(Transaction transaction)
{
    _logger.LogDatabaseOperation("Create", "Transaction", transaction.Id);
    
    await _context.Transactions.AddAsync(transaction);
    await _context.SaveChangesAsync();
    
    _logger.LogDatabaseOperation("Created", "Transaction", transaction.Id);
    return transaction;
}
```

### Service
```csharp
public async Task<Result> ProcessOperation(Input input)
{
    _logger.LogOperationStart("ProcessOperation", new { InputId = input.Id });
    
    try
    {
        // Business logic
        var result = await DoWork(input);
        
        _logger.LogOperationSuccess("ProcessOperation", new { ResultId = result.Id });
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process operation {InputId}", input.Id);
        throw;
    }
}
```

