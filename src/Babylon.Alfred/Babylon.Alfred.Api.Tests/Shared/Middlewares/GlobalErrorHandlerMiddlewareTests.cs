using System.Text.Json;
using Babylon.Alfred.Api.Shared.Middlewares;
using Babylon.Alfred.Api.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Babylon.Alfred.Api.Tests.Shared.Middlewares;

public class GlobalErrorHandlerMiddlewareTests
{
    private static async Task<(int statusCode, ApiResponse<object>? body)> InvokeWithException(Exception ex)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalErrorHandlerMiddleware(
            _ => throw ex,
            NullLogger<GlobalErrorHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var body = JsonSerializer.Deserialize<ApiResponse<object>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task ArgumentException_Returns400()
    {
        var (status, body) = await InvokeWithException(new ArgumentException("invalid amount"));

        status.Should().Be(400);
        body!.Success.Should().BeFalse();
        body.Error.Should().Be("invalid amount");
    }

    [Fact]
    public async Task KeyNotFoundException_Returns404()
    {
        var (status, body) = await InvokeWithException(new KeyNotFoundException("not found"));

        status.Should().Be(404);
        body!.Success.Should().BeFalse();
        body.Error.Should().Be("not found");
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns401()
    {
        var (status, body) = await InvokeWithException(new UnauthorizedAccessException("unauthorized"));

        status.Should().Be(401);
        body!.Success.Should().BeFalse();
        body.Error.Should().Be("unauthorized");
    }

    [Fact]
    public async Task UnhandledException_Returns500WithGenericMessage()
    {
        var (status, body) = await InvokeWithException(new InvalidOperationException("internal detail"));

        status.Should().Be(500);
        body!.Success.Should().BeFalse();
        body.Error.Should().Be("An unexpected error occurred");
    }

    [Fact]
    public async Task NoException_PassesThrough()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalErrorHandlerMiddleware(
            ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; },
            NullLogger<GlobalErrorHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }
}
