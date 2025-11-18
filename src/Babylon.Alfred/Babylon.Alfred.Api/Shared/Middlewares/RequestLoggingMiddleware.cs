using System.Diagnostics;
using Babylon.Alfred.Api.Shared.Logging;

namespace Babylon.Alfred.Api.Shared.Middlewares;

/// <summary>
/// Middleware to log all API requests and responses with timing information.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;

        // Extract user ID from claims if available
        var userId = context.User?.FindFirst("sub")?.Value;
#pragma warning disable CS8602 // Dereference of a possibly null reference - FindFirst can return null
        Guid? userIdGuid = userId != null && Guid.TryParse(userId, out var guid) ? guid : null;
#pragma warning restore CS8602

        // Log request
        logger.LogApiRequest(method, path, userIdGuid);

        // Capture original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await next(context);

            stopwatch.Stop();

            // Log response
            logger.LogApiResponse(
                method,
                path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Unhandled exception in request pipeline for {Method} {Path}", method, path);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}

