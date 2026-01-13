using Babylon.Alfred.Api.Features.Authentication.Controllers;
using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Features.Authentication.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Features.Authentication.Controllers;

public class AuthControllerTests
{
    private readonly AutoMocker autoMocker = new();
    private readonly AuthController sut;

    public AuthControllerTests()
    {
        sut = autoMocker.CreateInstance<AuthController>();
    }

    [Fact]
    public async Task Refresh_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid-token" };
        var response = new AuthResponse { Token = "new-jwt", RefreshToken = "new-refresh" };

        autoMocker.GetMock<IAuthService>()
            .Setup(x => x.RefreshTokenAsync(request.RefreshToken))
            .ReturnsAsync(response);

        // Act
        var result = await sut.Refresh(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "invalid-token" };

        autoMocker.GetMock<IAuthService>()
            .Setup(x => x.RefreshTokenAsync(request.RefreshToken))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid token"));

        // Act
        var result = await sut.Refresh(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Logout_ShouldReturnOk()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid-token" };

        // Act
        var result = await sut.Logout(request);

        // Assert
        result.Should().BeOfType<OkResult>();
        autoMocker.GetMock<IAuthService>().Verify(x => x.LogoutAsync(request.RefreshToken), Times.Once);
    }
}
