using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Babylon.Alfred.Api.IntegrationTests.Integration;

/// <summary>
/// Repository tests against the real ephemeral Postgres container — exercises constraints
/// EF Core's InMemory provider silently ignores (partial unique indexes, foreign keys),
/// unlike the mocked/InMemory repository tests in Babylon.Alfred.Api.Tests.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class TransactionRepositoryIntegrationTests(BabylonApiFactory factory)
{
    [Fact]
    public async Task ImportFingerprint_DuplicateForSameUser_ViolatesRealUniqueIndex()
    {
        // Arrange
        var security = await factory.SeedSecurityAsync($"DUP{Guid.NewGuid():N}"[..8].ToUpperInvariant(), "Duplicate Fingerprint Test Co");
        var userId = await SeedUserAsync();
        var fingerprint = Guid.NewGuid().ToString();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BabylonDbContext>();

        context.Transactions.Add(NewBuyTransaction(userId, security.Id, fingerprint));
        await context.SaveChangesAsync();

        context.Transactions.Add(NewBuyTransaction(userId, security.Id, fingerprint));

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert — the partial unique index on (UserId, ImportFingerprint) rejects the duplicate.
        // EF InMemory would have silently allowed this.
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GetExistingImportFingerprintsAsync_OnlyReturnsFingerprintsAlreadyPersistedForThatUser()
    {
        // Arrange
        var security = await factory.SeedSecurityAsync($"GEF{Guid.NewGuid():N}"[..8].ToUpperInvariant(), "GetExisting Fingerprints Test Co");
        var userId = await SeedUserAsync();
        var otherUserId = await SeedUserAsync();
        var persistedFingerprint = Guid.NewGuid().ToString();
        var otherUsersFingerprint = Guid.NewGuid().ToString();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BabylonDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

        context.Transactions.Add(NewBuyTransaction(userId, security.Id, persistedFingerprint));
        context.Transactions.Add(NewBuyTransaction(otherUserId, security.Id, otherUsersFingerprint));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetExistingImportFingerprintsAsync(
            userId, [persistedFingerprint, otherUsersFingerprint, Guid.NewGuid().ToString()]);

        // Assert
        result.Should().BeEquivalentTo([persistedFingerprint]);
    }

    private async Task<Guid> SeedUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BabylonDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"user_{Guid.NewGuid():N}"[..20],
            Email = $"{Guid.NewGuid()}@example.com",
            Password = "irrelevant-for-this-test"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private static Transaction NewBuyTransaction(Guid userId, Guid securityId, string fingerprint) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        SecurityId = securityId,
        TransactionType = TransactionType.Buy,
        Date = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        SharesQuantity = 1m,
        SharePrice = 1m,
        ImportSource = "TradeRepublic",
        ImportFingerprint = fingerprint
    };
}
