using Babylon.Alfred.Api.Shared.Models;
using Newtonsoft.Json;

namespace Babylon.Alfred.Api.Shared.Middlewares;

public class GlobalErrorHandlerMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, logger);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
    {
        // Extract user ID from claims if available
        var userId = context.User?.FindFirst("sub")?.Value;
        Guid? userIdGuid = Guid.TryParse(userId, out var guid) ? guid : null;

        // Log exception with full context
        logger.LogError(
            exception,
            "Unhandled exception: {ExceptionType} - {Message} | Method: {Method} | Path: {Path} | UserId: {UserId}",
            exception.GetType().Name,
            exception.Message,
            context.Request.Method,
            context.Request.Path + context.Request.QueryString,
            userIdGuid);

        // Log inner exception if present
        if (exception.InnerException != null)
        {
            logger.LogError(
                exception.InnerException,
                "Inner exception: {ExceptionType} - {Message}",
                exception.InnerException.GetType().Name,
                exception.InnerException.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new ApiErrorResponse
        {
            Success = false,
            Message = "An unexpected error has occurred",
            Errors = [new {name = "InternalServerError", message = exception.Message}]
        };

        return context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
}
