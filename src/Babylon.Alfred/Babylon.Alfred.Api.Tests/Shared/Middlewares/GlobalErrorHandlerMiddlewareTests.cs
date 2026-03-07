using System.Text.Json;
using Babylon.Alfred.Api.Shared.Middlewares;
using Babylon.Alfred.Api.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Shared.Middlewares;

public class GlobalErrorHandlerMiddlewareTests
{
    private readonly Mock<ILogger<GlobalErrorHandlerMiddleware>> loggerMock = new();

    private GlobalErrorHandlerMiddleware CreateSut(RequestDelegate next)
    {
        return new GlobalErrorHandlerMiddleware(next, loggerMock.Object);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ApiErrorResponse?> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<ApiErrorResponse>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessExceptionThrown_ShouldReturn401()
    {
        // Arrange
        RequestDelegate next = _ => throw new UnauthorizedAccessException("Invalid current password");
        var context = CreateHttpContext();
        var sut = CreateSut(next);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessExceptionThrown_ShouldReturnErrorResponse()
    {
        // Arrange
        RequestDelegate next = _ => throw new UnauthorizedAccessException("Invalid current password");
        var context = CreateHttpContext();
        var sut = CreateSut(next);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        var response = await ReadResponseBody(context);
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationExceptionThrown_ShouldReturn400()
    {
        // Arrange
        RequestDelegate next = _ => throw new InvalidOperationException("User not found.");
        var context = CreateHttpContext();
        var sut = CreateSut(next);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationExceptionThrown_ShouldReturnErrorResponse()
    {
        // Arrange
        RequestDelegate next = _ => throw new InvalidOperationException("User not found.");
        var context = CreateHttpContext();
        var sut = CreateSut(next);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        var response = await ReadResponseBody(context);
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenGenericExceptionThrown_ShouldReturn500()
    {
        // Arrange
        RequestDelegate next = _ => throw new Exception("Unexpected error");
        var context = CreateHttpContext();
        var sut = CreateSut(next);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoExceptionThrown_ShouldNotModifyResponse()
    {
        // Arrange
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };
        var context = CreateHttpContext();
        var sut = CreateSut(next);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
