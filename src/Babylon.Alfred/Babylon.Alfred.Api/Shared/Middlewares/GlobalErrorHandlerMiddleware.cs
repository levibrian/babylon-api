using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
            logger.LogError(
                ex,
                "Unhandled exception: {ExceptionType} | {Method} {Path} | UserId: {UserId}",
                ex.GetType().Name,
                context.Request.Method,
                context.Request.Path,
                context.User?.FindFirst("sub")?.Value);

            context.Response.StatusCode = ex switch
            {
                ArgumentException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            context.Response.ContentType = "application/json";

            var error = context.Response.StatusCode == 500
                ? "An unexpected error occurred"
                : ex.Message;

            var response = ApiResponse<object>.Fail(error);
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
    }
}
