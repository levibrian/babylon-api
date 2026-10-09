using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Tests.Shared.Repositories;

public class AllocationStrategyRepositoryTests : IDisposable
{
    private readonly BabylonDbContext context;
    private readonly AllocationStrategyRepository sut;

    public AllocationStrategyRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new BabylonDbContext(options);
        sut = new AllocationStrategyRepository(context);
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenUserHasTargets_ShouldReturnThemWithSecurity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var security = await AddSecurityAsync("VWCE");
        await AddStrategyAsync(userId, security.Id, 60m);

        // Act
        var result = await sut.GetByUserIdAsync(userId);

        // Assert
        result.Should().ContainSingle();
        result[0].Security.Ticker.Should().Be("VWCE");
        result[0].TargetPercentage.Should().Be(60m);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldNeverReturnAnotherUsersTargets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var security = await AddSecurityAsync("VWCE");
        await AddStrategyAsync(otherUserId, security.Id, 60m);

        // Act
        var result = await sut.GetByUserIdAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReplaceForUserAsync_ShouldUpdateExistingAddNewAndRemoveMissing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kept = await AddSecurityAsync("VWCE");
        var removed = await AddSecurityAsync("AAPL");
        var added = await AddSecurityAsync("BTC");
        var keptId = await AddStrategyAsync(userId, kept.Id, 60m);
        await AddStrategyAsync(userId, removed.Id, 20m);

        var replacement = new List<AllocationStrategy>
        {
            NewStrategy(userId, kept.Id, 70m),
            NewStrategy(userId, added.Id, 10m)
        };

        // Act
        await sut.ReplaceForUserAsync(userId, replacement);

        // Assert
        var stored = await context.AllocationStrategies.AsNoTracking().Where(a => a.UserId == userId).ToListAsync();
        stored.Should().HaveCount(2);
        stored.Should().ContainSingle(a => a.SecurityId == kept.Id && a.TargetPercentage == 70m && a.Id == keptId);
        stored.Should().ContainSingle(a => a.SecurityId == added.Id && a.TargetPercentage == 10m);
    }

    [Fact]
    public async Task ReplaceForUserAsync_ShouldNotTouchAnotherUsersTargets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var security = await AddSecurityAsync("VWCE");
        await AddStrategyAsync(otherUserId, security.Id, 60m);

        // Act
        await sut.ReplaceForUserAsync(userId, new List<AllocationStrategy>());

        // Assert
        var otherStored = await context.AllocationStrategies.AsNoTracking().Where(a => a.UserId == otherUserId).ToListAsync();
        otherStored.Should().ContainSingle(a => a.TargetPercentage == 60m);
    }

    private async Task<Security> AddSecurityAsync(string ticker)
    {
        var security = new Security { Id = Guid.NewGuid(), Ticker = ticker, SecurityName = ticker };
        context.Securities.Add(security);
        await context.SaveChangesAsync();
        return security;
    }

    private async Task<Guid> AddStrategyAsync(Guid userId, Guid securityId, decimal target)
    {
        var strategy = NewStrategy(userId, securityId, target);
        strategy.Id = Guid.NewGuid();
        context.AllocationStrategies.Add(strategy);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return strategy.Id;
    }

    private static AllocationStrategy NewStrategy(Guid userId, Guid securityId, decimal target) => new()
    {
        UserId = userId,
        SecurityId = securityId,
        TargetPercentage = target,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
