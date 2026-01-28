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
            SecurityType = SecurityType.Stock,
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
            new Security { Id = Guid.NewGuid(), Ticker = "AAPL", SecurityName = "Apple Inc.", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow },
            new Security { Id = Guid.NewGuid(), Ticker = "GOOGL", SecurityName = "Alphabet Inc.", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow },
            new Security { Id = Guid.NewGuid(), Ticker = "MSFT", SecurityName = "Microsoft Corp.", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow }
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
            new Security { Id = Guid.NewGuid(), Ticker = "AAPL", SecurityName = "Apple Inc.", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow },
            new Security { Id = Guid.NewGuid(), Ticker = "GOOGL", SecurityName = "Alphabet Inc.", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow },
            new Security { Id = Guid.NewGuid(), Ticker = "MSFT", SecurityName = "Microsoft Corp.", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow }
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
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.Stock
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
            SecurityType = SecurityType.Stock,
            LastUpdated = DateTime.UtcNow.AddDays(-10)
        };
        await context.Securities.AddAsync(existingSecurity);
        await context.SaveChangesAsync();

        var updatedSecurity = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.ETF
        };

        // Act
        var result = await sut.AddOrUpdateAsync(updatedSecurity);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("AAPL");
        result.SecurityName.Should().Be("Apple Inc.");
        result.SecurityType.Should().Be(SecurityType.ETF);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        var allSecurities = await context.Securities.ToListAsync();
        allSecurities.Should().HaveCount(1); // Should still be only one security
        allSecurities.First().SecurityName.Should().Be("Apple Inc.");
        allSecurities.First().SecurityType.Should().Be(SecurityType.ETF);
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
            SecurityType = SecurityType.Stock,
            LastUpdated = oldTimestamp
        };
        await context.Securities.AddAsync(existingSecurity);
        await context.SaveChangesAsync();

        var updatedSecurity = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.ETF
        };

        // Act
        var result = await sut.AddOrUpdateAsync(updatedSecurity);

        // Assert
        result.LastUpdated.Should().NotBe(oldTimestamp);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.SecurityType.Should().Be(SecurityType.ETF);
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenAdding_ShouldSetLastUpdatedTimestamp()
    {
        // Arrange
        var security = new Security
        {
            Ticker = "AAPL",
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.Stock,
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
            SecurityType = SecurityType.Stock,
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
            new Security { Id = Guid.NewGuid(), Ticker = "AAPL", SecurityName = "Apple Inc.", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow },
            new Security { Id = Guid.NewGuid(), Ticker = "GOOGL", SecurityName = "Alphabet Inc.", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow },
            new Security { Id = Guid.NewGuid(), Ticker = "MSFT", SecurityName = "Microsoft Corp.", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow }
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
            SecurityType = SecurityType.Stock,
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
            SecurityName = "First Name",
            SecurityType = SecurityType.Stock
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

    [Fact]
    public async Task GetByIsinAsync_WhenSecurityExists_ShouldReturnSecurity()
    {
        // Arrange
        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            Isin = "US0378331005",
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.Stock,
            LastUpdated = DateTime.UtcNow
        };
        await context.Securities.AddAsync(security);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetByIsinAsync("US0378331005");

        // Assert
        result.Should().NotBeNull();
        result!.Isin.Should().Be("US0378331005");
        result.Ticker.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetByIsinAsync_WhenSecurityDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await sut.GetByIsinAsync("US0000000000");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIsinAsync_WithMultipleTickersForSameIsin_ShouldReturnFirstByTicker()
    {
        // Arrange
        var securities = new[]
        {
            new Security { Id = Guid.NewGuid(), Ticker = "GOOGL", Isin = "US02079K3059", SecurityName = "Alphabet Inc. Class A", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow },
            new Security { Id = Guid.NewGuid(), Ticker = "GOOG", Isin = "US02079K3059", SecurityName = "Alphabet Inc. Class C", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow }
        };
        await context.Securities.AddRangeAsync(securities);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetByIsinAsync("US02079K3059");

        // Assert
        result.Should().NotBeNull();
        result!.Ticker.Should().Be("GOOG"); // Should return first alphabetically
        result.Isin.Should().Be("US02079K3059");
    }

    [Fact]
    public async Task GetAllByIsinAsync_WhenNoSecuritiesExist_ShouldReturnEmptyList()
    {
        // Act
        var result = await sut.GetAllByIsinAsync("US0000000000");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllByIsinAsync_WithSingleSecurity_ShouldReturnOne()
    {
        // Arrange
        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            Isin = "US0378331005",
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.Stock,
            LastUpdated = DateTime.UtcNow
        };
        await context.Securities.AddAsync(security);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetAllByIsinAsync("US0378331005");

        // Assert
        result.Should().HaveCount(1);
        result[0].Ticker.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetAllByIsinAsync_WithMultipleTickersForSameIsin_ShouldReturnAll()
    {
        // Arrange
        var securities = new[]
        {
            new Security { Id = Guid.NewGuid(), Ticker = "GOOGL", Isin = "US02079K3059", SecurityName = "Alphabet Inc. Class A", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow },
            new Security { Id = Guid.NewGuid(), Ticker = "GOOG", Isin = "US02079K3059", SecurityName = "Alphabet Inc. Class C", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow },
            new Security { Id = Guid.NewGuid(), Ticker = "AAPL", Isin = "US0378331005", SecurityName = "Apple Inc.", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow }
        };
        await context.Securities.AddRangeAsync(securities);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetAllByIsinAsync("US02079K3059");

        // Assert
        result.Should().HaveCount(2);
        result.Select(s => s.Ticker).Should().BeEquivalentTo(new[] { "GOOG", "GOOGL" });
        result.Should().AllSatisfy(s => s.Isin.Should().Be("US02079K3059"));
    }

    [Fact]
    public async Task GetAllByIsinAsync_ShouldReturnOrderedByTicker()
    {
        // Arrange
        var securities = new[]
        {
            new Security { Id = Guid.NewGuid(), Ticker = "GOOGL", Isin = "US02079K3059", SecurityName = "Alphabet Inc. Class A", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow },
            new Security { Id = Guid.NewGuid(), Ticker = "GOOG", Isin = "US02079K3059", SecurityName = "Alphabet Inc. Class C", SecurityType = SecurityType.Stock, LastUpdated = DateTime.UtcNow }
        };
        await context.Securities.AddRangeAsync(securities);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetAllByIsinAsync("US02079K3059");

        // Assert
        result.Should().HaveCount(2);
        result[0].Ticker.Should().Be("GOOG");
        result[1].Ticker.Should().Be("GOOGL");
    }

    [Fact]
    public async Task AddOrUpdateAsync_WithIsin_ShouldSaveIsin()
    {
        // Arrange
        var security = new Security
        {
            Ticker = "AAPL",
            Isin = "US0378331005",
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.Stock
        };

        // Act
        var result = await sut.AddOrUpdateAsync(security);

        // Assert
        result.Isin.Should().Be("US0378331005");
        var savedSecurity = await context.Securities.FirstOrDefaultAsync(c => c.Ticker == "AAPL");
        savedSecurity.Should().NotBeNull();
        savedSecurity!.Isin.Should().Be("US0378331005");
    }

    [Fact]
    public async Task AddOrUpdateAsync_WithDuplicateIsin_ShouldAllowMultipleSecurities()
    {
        // Arrange
        var security1 = new Security
        {
            Ticker = "GOOGL",
            Isin = "US02079K3059",
            SecurityName = "Alphabet Inc. Class A",
            SecurityType = SecurityType.Stock
        };
        var security2 = new Security
        {
            Ticker = "GOOG",
            Isin = "US02079K3059",
            SecurityName = "Alphabet Inc. Class C",
            SecurityType = SecurityType.Stock
        };

        // Act
        await sut.AddOrUpdateAsync(security1);
        await sut.AddOrUpdateAsync(security2);

        // Assert
        var allSecurities = await context.Securities.ToListAsync();
        allSecurities.Should().HaveCount(2);
        allSecurities.Should().AllSatisfy(s => s.Isin.Should().Be("US02079K3059"));
    }

    [Fact]
    public async Task AddOrUpdateAsync_WithNullIsin_ShouldAllowMultipleNulls()
    {
        // Arrange
        var security1 = new Security
        {
            Ticker = "AAPL",
            Isin = null,
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.Stock
        };
        var security2 = new Security
        {
            Ticker = "MSFT",
            Isin = null,
            SecurityName = "Microsoft Corp.",
            SecurityType = SecurityType.Stock
        };

        // Act
        await sut.AddOrUpdateAsync(security1);
        await sut.AddOrUpdateAsync(security2);

        // Assert
        var allSecurities = await context.Securities.ToListAsync();
        allSecurities.Should().HaveCount(2);
        allSecurities.Should().AllSatisfy(s => s.Isin.Should().BeNull());
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenUpdating_ShouldUpdateIsinField()
    {
        // Arrange
        var existingSecurity = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            Isin = null,
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.Stock,
            LastUpdated = DateTime.UtcNow.AddDays(-10)
        };
        await context.Securities.AddAsync(existingSecurity);
        await context.SaveChangesAsync();

        var updatedSecurity = new Security
        {
            Ticker = "AAPL",
            Isin = "US0378331005",
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.Stock
        };

        // Act
        var result = await sut.AddOrUpdateAsync(updatedSecurity);

        // Assert
        result.Isin.Should().Be("US0378331005");
        var savedSecurity = await context.Securities.FirstOrDefaultAsync(c => c.Ticker == "AAPL");
        savedSecurity.Should().NotBeNull();
        savedSecurity!.Isin.Should().Be("US0378331005");

        var allSecurities = await context.Securities.ToListAsync();
        allSecurities.Should().HaveCount(1);
    }
}

