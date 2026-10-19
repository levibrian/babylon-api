using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Babylon.Alfred.Api.Tests.Shared.Repositories;

public class MarketPriceRepositoryTests : IDisposable
{
    private readonly BabylonDbContext context;
    private readonly MarketPriceRepository sut;

    public MarketPriceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new BabylonDbContext(options);
        sut = new MarketPriceRepository(context, NullLogger<MarketPriceRepository>.Instance);
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    [Fact]
    public async Task GetSecuritiesNeedingUpdateAsync_WhenSecurityIsHeldButHasNoTarget_ShouldIncludeIt()
    {
        // Arrange
        var security = await AddSecurityAsync("AAPL");
        await AddBuyAsync(Guid.NewGuid(), security.Id, shares: 10m);

        // Act
        var result = await sut.GetSecuritiesNeedingUpdateAsync(TimeSpan.FromHours(1));

        // Assert
        result.Should().ContainSingle(s => s.Id == security.Id);
    }

    [Fact]
    public async Task GetSecuritiesNeedingUpdateAsync_WhenAllSharesSold_ShouldNotIncludeIt()
    {
        // Arrange
        var security = await AddSecurityAsync("AAPL");
        var userId = Guid.NewGuid();
        await AddBuyAsync(userId, security.Id, shares: 10m);
        await AddSellAsync(userId, security.Id, shares: 10m);

        // Act
        var result = await sut.GetSecuritiesNeedingUpdateAsync(TimeSpan.FromHours(1));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSecuritiesNeedingUpdateAsync_WhenSecurityHasTargetButIsUnheld_ShouldStillIncludeIt()
    {
        // Arrange
        var security = await AddSecurityAsync("VOO");
        await AddStrategyAsync(Guid.NewGuid(), security.Id, 20m);

        // Act
        var result = await sut.GetSecuritiesNeedingUpdateAsync(TimeSpan.FromHours(1));

        // Assert
        result.Should().ContainSingle(s => s.Id == security.Id);
    }

    private async Task<Security> AddSecurityAsync(string ticker)
    {
        var security = new Security { Id = Guid.NewGuid(), Ticker = ticker, SecurityName = ticker };
        context.Securities.Add(security);
        await context.SaveChangesAsync();
        return security;
    }

    private async Task AddBuyAsync(Guid userId, Guid securityId, decimal shares) =>
        await AddTransactionAsync(userId, securityId, TransactionType.Buy, shares);

    private async Task AddSellAsync(Guid userId, Guid securityId, decimal shares) =>
        await AddTransactionAsync(userId, securityId, TransactionType.Sell, shares);

    private async Task AddTransactionAsync(Guid userId, Guid securityId, TransactionType type, decimal shares)
    {
        context.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SecurityId = securityId,
            TransactionType = type,
            SharesQuantity = shares,
            SharePrice = 100m,
            Date = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private async Task AddStrategyAsync(Guid userId, Guid securityId, decimal target)
    {
        context.AllocationStrategies.Add(new AllocationStrategy
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SecurityId = securityId,
            TargetPercentage = target,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }
}
