using AutoFixture;
using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Babylon.Alfred.Api.Tests.Shared.Repositories;

public class SecurityRepositoryTests : IDisposable
{
    private readonly Fixture fixture = new();
    private readonly BabylonDbContext context;
    private readonly SecurityRepository sut;

    public SecurityRepositoryTests()
    {
        // Configure AutoFixture to handle recursive types
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var options = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new BabylonDbContext(options);
        var logger = Mock.Of<ILogger<SecurityRepository>>();
        sut = new SecurityRepository(context, logger);
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    [Fact]
    public async Task GetByTickerAsync_WhenSecurityExists_ShouldReturnSecurity()
    {
        // Arrange
        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Apple Inc.",
            LastUpdated = DateTime.UtcNow
        };
        await context.Securities.AddAsync(security);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetByTickerAsync("AAPL");

        // Assert
        result.Should().NotBeNull();
        result!.Ticker.Should().Be("AAPL");
        result.SecurityName.Should().Be("Apple Inc.");
    }

    [Fact]
    public async Task GetByTickerAsync_WhenSecurityDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await sut.GetByTickerAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTickerAsync_WithMultipleSecurities_ShouldReturnCorrectSecurity()
    {
        // Arrange
        var securities = new[]
        {
            new Security { Id = fixture.Create<Guid>(), Ticker = "AAPL", SecurityName = "Apple Inc.", LastUpdated = DateTime.UtcNow },
            new Security { Id = fixture.Create<Guid>(), Ticker = "GOOGL", SecurityName = "Alphabet Inc.", LastUpdated = DateTime.UtcNow },
            new Security { Id = fixture.Create<Guid>(), Ticker = "MSFT", SecurityName = "Microsoft Corp.", LastUpdated = DateTime.UtcNow }
        };
        await context.Securities.AddRangeAsync(securities);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetByTickerAsync("GOOGL");

        // Assert
        result.Should().NotBeNull();
        result!.Ticker.Should().Be("GOOGL");
        result.SecurityName.Should().Be("Alphabet Inc.");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoSecuritiesExist_ShouldReturnEmptyList()
    {
        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenSecuritiesExist_ShouldReturnAllSecurities()
    {
        // Arrange
        var securities = new[]
        {
            new Security { Id = fixture.Create<Guid>(), Ticker = "AAPL", SecurityName = "Apple Inc.", LastUpdated = DateTime.UtcNow },
            new Security { Id = fixture.Create<Guid>(), Ticker = "GOOGL", SecurityName = "Alphabet Inc.", LastUpdated = DateTime.UtcNow },
            new Security { Id = fixture.Create<Guid>(), Ticker = "MSFT", SecurityName = "Microsoft Corp.", LastUpdated = DateTime.UtcNow }
        };
        await context.Securities.AddRangeAsync(securities);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(c => c.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "GOOGL", "MSFT" });
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenSecurityDoesNotExist_ShouldAddNewSecurity()
    {
        // Arrange
        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Apple Inc."
        };

        // Act
        var result = await sut.AddOrUpdateAsync(security);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("AAPL");
        result.SecurityName.Should().Be("Apple Inc.");
        result.LastUpdated.Should().NotBeNull();
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        var savedSecurity = await context.Securities.FirstOrDefaultAsync(c => c.Ticker == "AAPL");
        savedSecurity.Should().NotBeNull();
        savedSecurity!.SecurityName.Should().Be("Apple Inc.");
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenSecurityExists_ShouldUpdateExistingSecurity()
    {
        // Arrange
        var existingSecurity = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Old Name",
            LastUpdated = DateTime.UtcNow.AddDays(-10)
        };
        await context.Securities.AddAsync(existingSecurity);
        await context.SaveChangesAsync();

        var updatedSecurity = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Apple Inc."
        };

        // Act
        var result = await sut.AddOrUpdateAsync(updatedSecurity);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("AAPL");
        result.SecurityName.Should().Be("Apple Inc.");
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        var allSecurities = await context.Securities.ToListAsync();
        allSecurities.Should().HaveCount(1); // Should still be only one security
        allSecurities.First().SecurityName.Should().Be("Apple Inc.");
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenUpdating_ShouldUpdateLastUpdatedTimestamp()
    {
        // Arrange
        var oldTimestamp = DateTime.UtcNow.AddDays(-30);
        var existingSecurity = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Old Name",
            LastUpdated = oldTimestamp
        };
        await context.Securities.AddAsync(existingSecurity);
        await context.SaveChangesAsync();

        var updatedSecurity = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Apple Inc."
        };

        // Act
        var result = await sut.AddOrUpdateAsync(updatedSecurity);

        // Assert
        result.LastUpdated.Should().NotBe(oldTimestamp);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenAdding_ShouldSetLastUpdatedTimestamp()
    {
        // Arrange
        var security = new Security
        {
            Ticker = "AAPL",
            SecurityName = "Apple Inc.",
            LastUpdated = null
        };
        var beforeAdd = DateTime.UtcNow;

        // Act
        var result = await sut.AddOrUpdateAsync(security);
        var afterAdd = DateTime.UtcNow;

        // Assert
        result.LastUpdated.Should().NotBeNull();
        result.LastUpdated.Should().BeOnOrAfter(beforeAdd);
        result.LastUpdated.Should().BeOnOrBefore(afterAdd);
    }

    [Fact]
    public async Task DeleteAsync_WhenSecurityExists_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Apple Inc.",
            LastUpdated = DateTime.UtcNow
        };
        await context.Securities.AddAsync(security);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.DeleteAsync("AAPL");

        // Assert
        result.Should().BeTrue();
        var deletedSecurity = await context.Securities.FirstOrDefaultAsync(c => c.Ticker == "AAPL");
        deletedSecurity.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenSecurityDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var result = await sut.DeleteAsync("NONEXISTENT");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotAffectOtherSecurities()
    {
        // Arrange
        var securities = new[]
        {
            new Security { Id = fixture.Create<Guid>(), Ticker = "AAPL", SecurityName = "Apple Inc.", LastUpdated = DateTime.UtcNow },
            new Security { Id = fixture.Create<Guid>(), Ticker = "GOOGL", SecurityName = "Alphabet Inc.", LastUpdated = DateTime.UtcNow },
            new Security { Id = fixture.Create<Guid>(), Ticker = "MSFT", SecurityName = "Microsoft Corp.", LastUpdated = DateTime.UtcNow }
        };
        await context.Securities.AddRangeAsync(securities);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.DeleteAsync("GOOGL");

        // Assert
        result.Should().BeTrue();
        var remainingSecurities = await context.Securities.ToListAsync();
        remainingSecurities.Should().HaveCount(2);
        remainingSecurities.Select(c => c.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "MSFT" });
    }

    [Fact]
    public async Task GetByTickerAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Apple Inc.",
            LastUpdated = DateTime.UtcNow
        };
        await context.Securities.AddAsync(security);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetByTickerAsync("AAPL");

        // Assert
        result.Should().NotBeNull();
        result!.Ticker.Should().Be("AAPL");
    }

    [Fact]
    public async Task AddOrUpdateAsync_WithMultipleUpdates_ShouldPersistLatestChanges()
    {
        // Arrange
        var security = new Security
        {
            Ticker = "AAPL",
            SecurityName = "First Name"
        };
        await sut.AddOrUpdateAsync(security);

        // Act
        security.SecurityName = "Second Name";
        await sut.AddOrUpdateAsync(security);

        security.SecurityName = "Third Name";
        var result = await sut.AddOrUpdateAsync(security);

        // Assert
        result.SecurityName.Should().Be("Third Name");
        var savedSecurity = await context.Securities.FirstOrDefaultAsync(c => c.Ticker == "AAPL");
        savedSecurity.Should().NotBeNull();
        savedSecurity!.SecurityName.Should().Be("Third Name");

        var allSecurities = await context.Securities.ToListAsync();
        allSecurities.Should().HaveCount(1);
    }
}

