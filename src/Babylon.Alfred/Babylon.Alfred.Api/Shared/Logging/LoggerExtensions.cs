namespace Babylon.Alfred.Api.Shared.Logging;

/// <summary>
/// Extension methods for structured logging with consistent patterns.
/// Provides pragmatic logging that captures what happened without overload.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs entry into a method/operation with context.
    /// Use at the start of important operations.
    /// </summary>
    public static void LogOperationStart(this ILogger logger, string operation, object? context = null)
    {
        logger.LogInformation("Starting {Operation} {Context}", operation, context);
    }

    /// <summary>
    /// Logs successful completion of an operation with result summary.
    /// </summary>
    public static void LogOperationSuccess(this ILogger logger, string operation, object? result = null, object? context = null)
    {
        logger.LogInformation("Completed {Operation} successfully {Result} {Context}", operation, result, context);
    }

    /// <summary>
    /// Logs database operation with entity type and action.
    /// </summary>
    public static void LogDatabaseOperation(this ILogger logger, string operation, string entityType, object? identifier = null, int? count = null)
    {
        if (count.HasValue)
        {
            logger.LogInformation("Database {Operation} on {EntityType}: {Count} records {Identifier}", 
                operation, entityType, count.Value, identifier);
        }
        else
        {
            logger.LogInformation("Database {Operation} on {EntityType} {Identifier}", 
                operation, entityType, identifier);
        }
    }

    /// <summary>
    /// Logs API request with method, path, and user context.
    /// </summary>
    public static void LogApiRequest(this ILogger logger, string method, string path, Guid? userId = null, object? additionalContext = null)
    {
        logger.LogInformation("API Request: {Method} {Path} UserId: {UserId} {Context}", 
            method, path, userId, additionalContext);
    }

    /// <summary>
    /// Logs API response with status code and timing.
    /// </summary>
    public static void LogApiResponse(this ILogger logger, string method, string path, int statusCode, long elapsedMs, object? additionalContext = null)
    {
        var logLevel = statusCode >= 500 ? LogLevel.Error : statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
        logger.Log(logLevel, "API Response: {Method} {Path} Status: {StatusCode} Duration: {ElapsedMs}ms {Context}", 
            method, path, statusCode, elapsedMs, additionalContext);
    }

    /// <summary>
    /// Logs performance metrics for operations.
    /// </summary>
    public static void LogPerformance(this ILogger logger, string operation, long elapsedMs, object? context = null)
    {
        var logLevel = elapsedMs > 1000 ? LogLevel.Warning : LogLevel.Information;
        logger.Log(logLevel, "Performance: {Operation} took {ElapsedMs}ms {Context}", operation, elapsedMs, context);
    }

    /// <summary>
    /// Logs validation failures with details.
    /// </summary>
    public static void LogValidationFailure(this ILogger logger, string operation, string validationError, object? context = null)
    {
        logger.LogWarning("Validation failed for {Operation}: {ValidationError} {Context}", 
            operation, validationError, context);
    }

    /// <summary>
    /// Logs business rule violations.
    /// </summary>
    public static void LogBusinessRuleViolation(this ILogger logger, string operation, string rule, object? context = null)
    {
        logger.LogWarning("Business rule violation in {Operation}: {Rule} {Context}", 
            operation, rule, context);
    }
}

