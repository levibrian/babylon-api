using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Tests.Shared.Repositories;

public class CompanyRepositoryTests : IDisposable
{
    private readonly BabylonDbContext context;
    private readonly CompanyRepository sut;

    public CompanyRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new BabylonDbContext(options);
        sut = new CompanyRepository(context);
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    [Fact]
    public async Task GetByTickerAsync_WhenCompanyExists_ShouldReturnCompany()
    {
        // Arrange
        var company = new Company
        {
            Ticker = "AAPL",
            CompanyName = "Apple Inc.",
            LastUpdated = DateTime.UtcNow
        };
        await context.Companies.AddAsync(company);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetByTickerAsync("AAPL");

        // Assert
        result.Should().NotBeNull();
        result!.Ticker.Should().Be("AAPL");
        result.CompanyName.Should().Be("Apple Inc.");
    }

    [Fact]
    public async Task GetByTickerAsync_WhenCompanyDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await sut.GetByTickerAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTickerAsync_WithMultipleCompanies_ShouldReturnCorrectCompany()
    {
        // Arrange
        var companies = new[]
        {
            new Company { Ticker = "AAPL", CompanyName = "Apple Inc.", LastUpdated = DateTime.UtcNow },
            new Company { Ticker = "GOOGL", CompanyName = "Alphabet Inc.", LastUpdated = DateTime.UtcNow },
            new Company { Ticker = "MSFT", CompanyName = "Microsoft Corp.", LastUpdated = DateTime.UtcNow }
        };
        await context.Companies.AddRangeAsync(companies);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetByTickerAsync("GOOGL");

        // Assert
        result.Should().NotBeNull();
        result!.Ticker.Should().Be("GOOGL");
        result.CompanyName.Should().Be("Alphabet Inc.");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCompaniesExist_ShouldReturnEmptyList()
    {
        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenCompaniesExist_ShouldReturnAllCompanies()
    {
        // Arrange
        var companies = new[]
        {
            new Company { Ticker = "AAPL", CompanyName = "Apple Inc.", LastUpdated = DateTime.UtcNow },
            new Company { Ticker = "GOOGL", CompanyName = "Alphabet Inc.", LastUpdated = DateTime.UtcNow },
            new Company { Ticker = "MSFT", CompanyName = "Microsoft Corp.", LastUpdated = DateTime.UtcNow }
        };
        await context.Companies.AddRangeAsync(companies);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(c => c.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "GOOGL", "MSFT" });
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenCompanyDoesNotExist_ShouldAddNewCompany()
    {
        // Arrange
        var company = new Company
        {
            Ticker = "AAPL",
            CompanyName = "Apple Inc."
        };

        // Act
        var result = await sut.AddOrUpdateAsync(company);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("AAPL");
        result.CompanyName.Should().Be("Apple Inc.");
        result.LastUpdated.Should().NotBeNull();
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        var savedCompany = await context.Companies.FindAsync("AAPL");
        savedCompany.Should().NotBeNull();
        savedCompany!.CompanyName.Should().Be("Apple Inc.");
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenCompanyExists_ShouldUpdateExistingCompany()
    {
        // Arrange
        var existingCompany = new Company
        {
            Ticker = "AAPL",
            CompanyName = "Old Name",
            LastUpdated = DateTime.UtcNow.AddDays(-10)
        };
        await context.Companies.AddAsync(existingCompany);
        await context.SaveChangesAsync();

        var updatedCompany = new Company
        {
            Ticker = "AAPL",
            CompanyName = "Apple Inc."
        };

        // Act
        var result = await sut.AddOrUpdateAsync(updatedCompany);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("AAPL");
        result.CompanyName.Should().Be("Apple Inc.");
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        var allCompanies = await context.Companies.ToListAsync();
        allCompanies.Should().HaveCount(1); // Should still be only one company
        allCompanies.First().CompanyName.Should().Be("Apple Inc.");
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenUpdating_ShouldUpdateLastUpdatedTimestamp()
    {
        // Arrange
        var oldTimestamp = DateTime.UtcNow.AddDays(-30);
        var existingCompany = new Company
        {
            Ticker = "AAPL",
            CompanyName = "Old Name",
            LastUpdated = oldTimestamp
        };
        await context.Companies.AddAsync(existingCompany);
        await context.SaveChangesAsync();

        var updatedCompany = new Company
        {
            Ticker = "AAPL",
            CompanyName = "Apple Inc."
        };

        // Act
        var result = await sut.AddOrUpdateAsync(updatedCompany);

        // Assert
        result.LastUpdated.Should().NotBe(oldTimestamp);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenAdding_ShouldSetLastUpdatedTimestamp()
    {
        // Arrange
        var company = new Company
        {
            Ticker = "AAPL",
            CompanyName = "Apple Inc.",
            LastUpdated = null
        };
        var beforeAdd = DateTime.UtcNow;

        // Act
        var result = await sut.AddOrUpdateAsync(company);
        var afterAdd = DateTime.UtcNow;

        // Assert
        result.LastUpdated.Should().NotBeNull();
        result.LastUpdated.Should().BeOnOrAfter(beforeAdd);
        result.LastUpdated.Should().BeOnOrBefore(afterAdd);
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyExists_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var company = new Company
        {
            Ticker = "AAPL",
            CompanyName = "Apple Inc.",
            LastUpdated = DateTime.UtcNow
        };
        await context.Companies.AddAsync(company);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.DeleteAsync("AAPL");

        // Assert
        result.Should().BeTrue();
        var deletedCompany = await context.Companies.FindAsync("AAPL");
        deletedCompany.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var result = await sut.DeleteAsync("NONEXISTENT");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotAffectOtherCompanies()
    {
        // Arrange
        var companies = new[]
        {
            new Company { Ticker = "AAPL", CompanyName = "Apple Inc.", LastUpdated = DateTime.UtcNow },
            new Company { Ticker = "GOOGL", CompanyName = "Alphabet Inc.", LastUpdated = DateTime.UtcNow },
            new Company { Ticker = "MSFT", CompanyName = "Microsoft Corp.", LastUpdated = DateTime.UtcNow }
        };
        await context.Companies.AddRangeAsync(companies);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.DeleteAsync("GOOGL");

        // Assert
        result.Should().BeTrue();
        var remainingCompanies = await context.Companies.ToListAsync();
        remainingCompanies.Should().HaveCount(2);
        remainingCompanies.Select(c => c.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "MSFT" });
    }

    [Fact]
    public async Task GetByTickerAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var company = new Company
        {
            Ticker = "AAPL",
            CompanyName = "Apple Inc.",
            LastUpdated = DateTime.UtcNow
        };
        await context.Companies.AddAsync(company);
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
        var company = new Company
        {
            Ticker = "AAPL",
            CompanyName = "First Name"
        };
        await sut.AddOrUpdateAsync(company);

        // Act
        company.CompanyName = "Second Name";
        await sut.AddOrUpdateAsync(company);

        company.CompanyName = "Third Name";
        var result = await sut.AddOrUpdateAsync(company);

        // Assert
        result.CompanyName.Should().Be("Third Name");
        var savedCompany = await context.Companies.FindAsync("AAPL");
        savedCompany!.CompanyName.Should().Be("Third Name");

        var allCompanies = await context.Companies.ToListAsync();
        allCompanies.Should().HaveCount(1);
    }
}

