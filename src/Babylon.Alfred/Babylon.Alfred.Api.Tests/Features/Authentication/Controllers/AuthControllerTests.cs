using Babylon.Alfred.Api.Features.Authentication.Controllers;
using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Features.Authentication.Services;
using Babylon.Alfred.Api.Shared.Models;
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
    public async Task Refresh_WithValidRequest_ShouldReturnOkWithApiResponse()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid-token" };
        var authResponse = new AuthResponse { Token = "new-jwt", RefreshToken = "new-refresh" };

        autoMocker.GetMock<IAuthService>()
            .Setup(x => x.RefreshTokenAsync(request.RefreshToken))
            .ReturnsAsync(authResponse);

        // Act
        var result = await sut.Refresh(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AuthResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().BeEquivalentTo(authResponse);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ShouldThrowUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "invalid-token" };

        autoMocker.GetMock<IAuthService>()
            .Setup(x => x.RefreshTokenAsync(request.RefreshToken))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid token"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.Refresh(request));
    }

    [Fact]
    public async Task Logout_ShouldReturnOkWithApiResponse()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid-token" };

        // Act
        var result = await sut.Logout(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();
        autoMocker.GetMock<IAuthService>().Verify(x => x.LogoutAsync(request.RefreshToken), Times.Once);
    }
}
