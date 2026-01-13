using AutoFixture;
using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Features.Authentication.Services;
using Babylon.Alfred.Api.Features.Authentication.Utils;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Features.Authentication.Services;

public class AuthServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly AuthService sut;

    public AuthServiceTests()
    {
        // Configure AutoFixture to handle recursive types
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Authentication:Jwt:RefreshTokenExpirationDays"]).Returns("7");
        autoMocker.Use<IConfiguration>(configMock.Object);

        sut = autoMocker.CreateInstance<AuthService>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var refreshTokenStr = "valid-refresh-token";
        var user = fixture.Build<User>()
            .Without(u => u.Transactions)
            .Without(u => u.RefreshTokens)
            .Create();

        var storedToken = new RefreshToken
        {
            Token = refreshTokenStr,
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        };

        autoMocker.GetMock<IRefreshTokenRepository>()
            .Setup(x => x.GetByTokenAsync(refreshTokenStr))
            .ReturnsAsync(storedToken);

        autoMocker.GetMock<JwtTokenGenerator>()
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("new-jwt-token");

        autoMocker.GetMock<JwtTokenGenerator>()
            .Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        // Act
        var result = await sut.RefreshTokenAsync(refreshTokenStr);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("new-jwt-token");
        result.RefreshToken.Should().Be("new-refresh-token");
        result.UserId.Should().Be(user.Id);

        autoMocker.GetMock<IRefreshTokenRepository>().Verify(x => x.UpdateAsync(It.Is<RefreshToken>(t => t.IsRevoked)), Times.Once);
        autoMocker.GetMock<IRefreshTokenRepository>().Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldThrowUnauthorized()
    {
        // Arrange
        var refreshTokenStr = "expired-token";
        var storedToken = new RefreshToken
        {
            Token = refreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        };

        autoMocker.GetMock<IRefreshTokenRepository>()
            .Setup(x => x.GetByTokenAsync(refreshTokenStr))
            .ReturnsAsync(storedToken);

        // Act
        Func<Task> act = () => sut.RefreshTokenAsync(refreshTokenStr);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid or expired refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldThrowUnauthorized()
    {
        // Arrange
        var refreshTokenStr = "revoked-token";
        var storedToken = new RefreshToken
        {
            Token = refreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = true
        };

        autoMocker.GetMock<IRefreshTokenRepository>()
            .Setup(x => x.GetByTokenAsync(refreshTokenStr))
            .ReturnsAsync(storedToken);

        // Act
        Func<Task> act = () => sut.RefreshTokenAsync(refreshTokenStr);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid or expired refresh token");
    }

    [Fact]
    public async Task LogoutAsync_WithValidToken_ShouldRevokeToken()
    {
        // Arrange
        var refreshTokenStr = "valid-refresh-token";
        var storedToken = new RefreshToken
        {
            Token = refreshTokenStr,
            IsRevoked = false
        };

        autoMocker.GetMock<IRefreshTokenRepository>()
            .Setup(x => x.GetByTokenAsync(refreshTokenStr))
            .ReturnsAsync(storedToken);

        // Act
        await sut.LogoutAsync(refreshTokenStr);

        // Assert
        autoMocker.GetMock<IRefreshTokenRepository>().Verify(x => x.UpdateAsync(It.Is<RefreshToken>(t => t.IsRevoked == true)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldIncludeRefreshToken()
    {
        // Arrange
        var user = fixture.Build<User>()
            .With(u => u.Username, "testuser")
            .With(u => u.Password, BCrypt.Net.BCrypt.HashPassword("password"))
            .Without(u => u.Transactions)
            .Without(u => u.RefreshTokens)
            .Create();

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        autoMocker.GetMock<JwtTokenGenerator>()
            .Setup(x => x.GenerateToken(user))
            .Returns("jwt-token");

        autoMocker.GetMock<JwtTokenGenerator>()
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await sut.LoginAsync("testuser", "password");

        // Assert
        result.RefreshToken.Should().Be("refresh-token");
        autoMocker.GetMock<IRefreshTokenRepository>().Verify(x => x.RevokeAllUserTokensAsync(user.Id), Times.Once);
        autoMocker.GetMock<IRefreshTokenRepository>().Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }
}
