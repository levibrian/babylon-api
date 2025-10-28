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
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
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
