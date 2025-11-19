using AutoFixture;
using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Babylon.Alfred.Api.Tests.Shared.Repositories;

public class TransactionRepositoryTests : IDisposable
{
    private readonly Fixture fixture = new();
    private readonly BabylonDbContext context;
    private readonly TransactionRepository sut;

    public TransactionRepositoryTests()
    {
        // Configure AutoFixture to handle recursive types
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var options = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new BabylonDbContext(options);
        var logger = Mock.Of<ILogger<TransactionRepository>>();
        sut = new TransactionRepository(context, logger);
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    private async Task<Security> CreateSecurityAsync(string ticker, string securityName = null!)
    {
        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            SecurityName = securityName ?? $"{ticker} Inc."
        };
        await context.Securities.AddAsync(security);
        await context.SaveChangesAsync();
        return security;
    }

    [Fact]
    public async Task Add_WithValidTransaction_ShouldAddAndReturnTransaction()
    {
        // Arrange
        var security = await CreateSecurityAsync("AAPL", "Apple Inc.");

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SecurityId = security.Id,
            TransactionType = TransactionType.Buy,
            Date = DateTime.UtcNow,
            SharesQuantity = 10m,
            SharePrice = 150m,
            Fees = 5m,
            UserId = Guid.NewGuid()
        };

        // Act
        var result = await sut.Add(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(transaction.Id);
        result.SecurityId.Should().Be(security.Id);
        result.SharesQuantity.Should().Be(10m);
        result.SharePrice.Should().Be(150m);

        var savedTransaction = await context.Transactions.FindAsync(transaction.Id);
        savedTransaction.Should().NotBeNull();
    }

    [Fact]
    public async Task Add_ShouldPersistAllProperties()
    {
        // Arrange
        var security = await CreateSecurityAsync("GOOGL", "Alphabet Inc.");

        var transactionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var date = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var transaction = new Transaction
        {
            Id = transactionId,
            SecurityId = security.Id,
            TransactionType = TransactionType.Sell,
            Date = date,
            SharesQuantity = 5m,
            SharePrice = 2800m,
            Fees = 10m,
            UserId = userId
        };

        // Act
        await sut.Add(transaction);

        // Assert
        var savedTransaction = await context.Transactions.FindAsync(transactionId);
        savedTransaction.Should().NotBeNull();
        savedTransaction!.SecurityId.Should().Be(security.Id);
        savedTransaction.TransactionType.Should().Be(TransactionType.Sell);
        savedTransaction.Date.Should().Be(date);
        savedTransaction.SharesQuantity.Should().Be(5m);
        savedTransaction.SharePrice.Should().Be(2800m);
        savedTransaction.Fees.Should().Be(10m);
        savedTransaction.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetAll_WhenNoTransactionsExist_ShouldReturnEmptyList()
    {
        // Act
        var result = await sut.GetAll();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WhenTransactionsExist_ShouldReturnAllTransactions()
    {
        // Arrange
        var security1 = await CreateSecurityAsync("AAPL");
        var security2 = await CreateSecurityAsync("GOOGL");
        var security3 = await CreateSecurityAsync("MSFT");
        var userId = Guid.NewGuid();
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security1.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 10m,
                SharePrice = 150m,
                Fees = 5m,
                UserId = userId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security2.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 5m,
                SharePrice = 2800m,
                Fees = 10m,
                UserId = userId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security3.Id,
                TransactionType = TransactionType.Sell,
                Date = DateTime.UtcNow,
                SharesQuantity = 20m,
                SharePrice = 300m,
                Fees = 8m,
                UserId = userId
            }
        };
        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetAll();

        // Assert
        result.Should().HaveCount(3);
        result.Select(t => t.SecurityId).Should().BeEquivalentTo(new[] { security1.Id, security2.Id, security3.Id });
    }

    [Fact]
    public async Task AddBulk_WithValidTransactions_ShouldAddAllTransactions()
    {
        // Arrange
        var security1 = await CreateSecurityAsync("AAPL");
        var security2 = await CreateSecurityAsync("GOOGL");
        var userId = Guid.NewGuid();
        var transactions = new List<Transaction?>
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security1.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 10m,
                SharePrice = 150m,
                Fees = 5m,
                UserId = userId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security2.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 5m,
                SharePrice = 2800m,
                Fees = 10m,
                UserId = userId
            }
        };

        // Act
        var result = await sut.AddBulk(transactions);

        // Assert
        result.Should().HaveCount(2);
        var savedTransactions = await context.Transactions.Include(t => t.Security).ToListAsync();
        savedTransactions.Should().HaveCount(2);
        savedTransactions.Select(t => t.Security.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "GOOGL" });
    }

    [Fact]
    public async Task AddBulk_WithEmptyList_ShouldNotAddAnyTransactions()
    {
        // Arrange
        var transactions = new List<Transaction?>();

        // Act
        var result = await sut.AddBulk(transactions);

        // Assert
        result.Should().BeEmpty();
        var savedTransactions = await context.Transactions.ToListAsync();
        savedTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task AddBulk_WithLargeNumberOfTransactions_ShouldAddAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var securities = new List<Security>();
        for (int i = 1; i <= 100; i++)
        {
            securities.Add(await CreateSecurityAsync($"TICK{i}"));
        }

        var transactions = Enumerable.Range(1, 100)
            .Select(i => (Transaction?)new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = securities[i - 1].Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = i,
                SharePrice = 100m + i,
                Fees = i * 0.1m,
                UserId = userId
            })
            .ToList();

        // Act
        var result = await sut.AddBulk(transactions);

        // Assert
        result.Should().HaveCount(100);
        var savedTransactions = await context.Transactions.ToListAsync();
        savedTransactions.Should().HaveCount(100);
    }

    [Fact]
    public async Task GetOpenPositionsByUser_WhenNoTransactionsExist_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await sut.GetOpenPositionsByUser(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOpenPositionsByUser_ShouldReturnOnlyBuyTransactions()
    {
        // Arrange
        var security1 = await CreateSecurityAsync("AAPL");
        var security2 = await CreateSecurityAsync("GOOGL");
        var security3 = await CreateSecurityAsync("MSFT");
        var userId = Guid.NewGuid();
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security1.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 10m,
                SharePrice = 150m,
                Fees = 5m,
                UserId = userId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security2.Id,
                TransactionType = TransactionType.Sell,
                Date = DateTime.UtcNow,
                SharesQuantity = 5m,
                SharePrice = 2800m,
                Fees = 10m,
                UserId = userId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security3.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 20m,
                SharePrice = 300m,
                Fees = 8m,
                UserId = userId
            }
        };
        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetOpenPositionsByUser(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.TransactionType.Should().Be(TransactionType.Buy));
        var resultWithSecurities = await context.Transactions
            .Include(t => t.Security)
            .Where(t => result.Select(r => r.Id).Contains(t.Id))
            .ToListAsync();
        resultWithSecurities.Select(t => t.Security.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "MSFT" });
    }

    [Fact]
    public async Task GetOpenPositionsByUser_ShouldReturnOnlyTransactionsForSpecifiedUser()
    {
        // Arrange
        var security1 = await CreateSecurityAsync("AAPL");
        var security2 = await CreateSecurityAsync("GOOGL");
        var security3 = await CreateSecurityAsync("MSFT");
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security1.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 10m,
                SharePrice = 150m,
                Fees = 5m,
                UserId = userId1
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security2.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 5m,
                SharePrice = 2800m,
                Fees = 10m,
                UserId = userId2
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security3.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 20m,
                SharePrice = 300m,
                Fees = 8m,
                UserId = userId1
            }
        };
        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetOpenPositionsByUser(userId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.UserId.Should().Be(userId1));
        var resultWithSecurities = await context.Transactions
            .Include(t => t.Security)
            .Where(t => result.Select(r => r.Id).Contains(t.Id))
            .ToListAsync();
        resultWithSecurities.Select(t => t.Security.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "MSFT" });
    }

    [Fact]
    public async Task GetOpenPositionsByUser_ShouldReturnTransactionsOrderedByDateDescending()
    {
        // Arrange
        var security1 = await CreateSecurityAsync("OLD");
        var security2 = await CreateSecurityAsync("NEW");
        var security3 = await CreateSecurityAsync("MIDDLE");
        var userId = Guid.NewGuid();
        var oldDate = new DateTime(2024, 1, 1);
        var middleDate = new DateTime(2024, 6, 1);
        var newDate = new DateTime(2025, 1, 1);
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security1.Id,
                TransactionType = TransactionType.Buy,
                Date = oldDate,
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 1m,
                UserId = userId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security2.Id,
                TransactionType = TransactionType.Buy,
                Date = newDate,
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 1m,
                UserId = userId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security3.Id,
                TransactionType = TransactionType.Buy,
                Date = middleDate,
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 1m,
                UserId = userId
            }
        };
        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = (await sut.GetOpenPositionsByUser(userId)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(t => t.Date);
        var resultWithSecurities = await context.Transactions
            .Include(t => t.Security)
            .Where(t => result.Select(r => r.Id).Contains(t.Id))
            .OrderByDescending(t => t.Date)
            .ToListAsync();
        resultWithSecurities[0].Security.Ticker.Should().Be("NEW");
        resultWithSecurities[1].Security.Ticker.Should().Be("MIDDLE");
        resultWithSecurities[2].Security.Ticker.Should().Be("OLD");
    }

    [Fact]
    public async Task GetOpenPositionsByUser_WithMixedTypesAndUsers_ShouldFilterCorrectly()
    {
        // Arrange
        var security1 = await CreateSecurityAsync("AAPL");
        var security2 = await CreateSecurityAsync("MSFT");
        var security3 = await CreateSecurityAsync("GOOGL");
        var security4 = await CreateSecurityAsync("TSLA");
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var transactions = new[]
        {
            // Target user's Buy transactions (should be included)
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security1.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 10m,
                SharePrice = 150m,
                Fees = 5m,
                UserId = targetUserId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security2.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 20m,
                SharePrice = 300m,
                Fees = 8m,
                UserId = targetUserId
            },
            // Target user's Sell transactions (should be excluded)
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security3.Id,
                TransactionType = TransactionType.Sell,
                Date = DateTime.UtcNow,
                SharesQuantity = 5m,
                SharePrice = 2800m,
                Fees = 10m,
                UserId = targetUserId
            },
            // Other user's Buy transactions (should be excluded)
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security4.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 15m,
                SharePrice = 700m,
                Fees = 7m,
                UserId = otherUserId
            }
        };
        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetOpenPositionsByUser(targetUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t =>
        {
            t.UserId.Should().Be(targetUserId);
            t.TransactionType.Should().Be(TransactionType.Buy);
        });
        var resultWithSecurities = await context.Transactions
            .Include(t => t.Security)
            .Where(t => result.Select(r => r.Id).Contains(t.Id))
            .ToListAsync();
        resultWithSecurities.Select(t => t.Security.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "MSFT" });
    }

    [Fact]
    public async Task Add_WithComputedProperties_ShouldCalculateCorrectly()
    {
        // Arrange
        var security = await CreateSecurityAsync("AAPL");
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SecurityId = security.Id,
            TransactionType = TransactionType.Buy,
            Date = DateTime.UtcNow,
            SharesQuantity = 10m,
            SharePrice = 150m,
            Fees = 5m,
            UserId = Guid.NewGuid()
        };

        // Act
        await sut.Add(transaction);

        // Assert
        var savedTransaction = await context.Transactions.FindAsync(transaction.Id);
        savedTransaction.Should().NotBeNull();
        savedTransaction!.Amount.Should().Be(1500m); // 10 * 150
        savedTransaction.TotalAmount.Should().Be(1505m); // 1500 + 5
    }

    [Fact]
    public async Task GetAll_WithMultipleUsers_ShouldReturnAllTransactions()
    {
        // Arrange
        var security1 = await CreateSecurityAsync("AAPL");
        var security2 = await CreateSecurityAsync("GOOGL");
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security1.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 10m,
                SharePrice = 150m,
                Fees = 5m,
                UserId = user1
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security2.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 5m,
                SharePrice = 2800m,
                Fees = 10m,
                UserId = user2
            }
        };
        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetAll();

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.UserId).Should().BeEquivalentTo(new[] { user1, user2 });
    }

    [Fact]
    public async Task GetAllByUser_WithUserId_ShouldReturnAllTransactionsForUserOrderedByDateDescending()
    {
        // Arrange
        var security1 = await CreateSecurityAsync("AAPL", "Apple Inc.");
        var security2 = await CreateSecurityAsync("GOOGL", "Alphabet Inc.");
        var userId = Guid.NewGuid();
        var otherUserId = fixture.Create<Guid>();

        var oldDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var newDate = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var middleDate = new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security1.Id,
                TransactionType = TransactionType.Buy,
                Date = oldDate,
                SharesQuantity = 10m,
                SharePrice = 150m,
                Fees = 5m,
                UserId = userId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security2.Id,
                TransactionType = TransactionType.Sell,
                Date = newDate,
                SharesQuantity = 5m,
                SharePrice = 2800m,
                Fees = 10m,
                UserId = userId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security1.Id,
                TransactionType = TransactionType.Buy,
                Date = middleDate,
                SharesQuantity = 20m,
                SharePrice = 160m,
                Fees = 8m,
                UserId = userId
            },
            // Transaction for different user (should not be returned)
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security1.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow,
                SharesQuantity = 15m,
                SharePrice = 200m,
                Fees = 5m,
                UserId = otherUserId
            }
        };
        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = (await sut.GetAllByUser(userId)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(t => t.UserId.Should().Be(userId));
        result.Should().BeInDescendingOrder(t => t.Date);
        result[0].Date.Should().Be(newDate);
        result[1].Date.Should().Be(middleDate);
        result[2].Date.Should().Be(oldDate);
    }

    [Fact]
    public async Task GetAllByUser_ShouldIncludeSecurityInformation()
    {
        // Arrange
        var security = await CreateSecurityAsync("MSFT", "Microsoft Corporation");
        var userId = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SecurityId = security.Id,
            TransactionType = TransactionType.Buy,
            Date = DateTime.UtcNow,
            SharesQuantity = 10m,
            SharePrice = 300m,
            Fees = 5m,
            UserId = userId
        };
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();

        // Act
        var result = (await sut.GetAllByUser(userId)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Security.Should().NotBeNull();
        result[0].Security.Ticker.Should().Be("MSFT");
        result[0].Security.SecurityName.Should().Be("Microsoft Corporation");
    }

    [Fact]
    public async Task GetAllByUser_WithNoTransactions_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await sut.GetAllByUser(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllByUser_ShouldReturnBothBuyAndSellTransactions()
    {
        // Arrange
        var security = await CreateSecurityAsync("AAPL", "Apple Inc.");
        var userId = Guid.NewGuid();
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security.Id,
                TransactionType = TransactionType.Buy,
                Date = DateTime.UtcNow.AddDays(-10),
                SharesQuantity = 10m,
                SharePrice = 150m,
                Fees = 5m,
                UserId = userId
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                SecurityId = security.Id,
                TransactionType = TransactionType.Sell,
                Date = DateTime.UtcNow,
                SharesQuantity = 5m,
                SharePrice = 160m,
                Fees = 5m,
                UserId = userId
            }
        };
        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = (await sut.GetAllByUser(userId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.TransactionType).Should().Contain(TransactionType.Buy);
        result.Select(t => t.TransactionType).Should().Contain(TransactionType.Sell);
    }
}
