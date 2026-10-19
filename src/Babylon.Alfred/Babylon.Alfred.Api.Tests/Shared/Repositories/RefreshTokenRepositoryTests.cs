using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Tests.Shared.Repositories;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly BabylonDbContext context;
    private readonly RefreshTokenRepository sut;

    public RefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new BabylonDbContext(options);
        sut = new RefreshTokenRepository(context);
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeOnlyRestTokens_AndLeaveMcpTokensUntouched()
    {
        // Arrange
        var userId = await AddUserAsync();
        var restToken = await AddTokenAsync(userId, clientId: null);
        var mcpToken = await AddTokenAsync(userId, clientId: "mcp-client-abc");

        // Act
        await sut.RevokeAllUserTokensAsync(userId);

        // Assert
        (await sut.GetByTokenAsync(restToken.Token))!.IsRevoked.Should().BeTrue();
        (await sut.GetByTokenAsync(mcpToken.Token))!.IsRevoked.Should().BeFalse();
    }

    private async Task<Guid> AddUserAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = Guid.NewGuid().ToString(),
            Email = $"{Guid.NewGuid()}@example.com",
            AuthProvider = "Local",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private async Task<RefreshToken> AddTokenAsync(Guid userId, string? clientId)
    {
        var token = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            UserId = userId,
            ClientId = clientId,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        };
        await sut.AddAsync(token);
        return token;
    }
}
