namespace Babylon.Alfred.Api.Shared.Middlewares;

public class GlobalErrorHandlerMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        logger.LogInformation("Incoming request: {Method} {Path}", context.Request.Method, context.Request.Path);
        logger.LogInformation("Request body: {RequestBody}", requestBody);

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                error = "An unexpected error occurred.",
                details = ex.Message
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}