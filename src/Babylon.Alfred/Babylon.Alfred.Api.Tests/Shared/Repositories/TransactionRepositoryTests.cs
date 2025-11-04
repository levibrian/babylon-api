using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Tests.Shared.Repositories;

public class TransactionRepositoryTests : IDisposable
{
    private readonly BabylonDbContext context;
    private readonly TransactionRepository sut;

    public TransactionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new BabylonDbContext(options);
        sut = new TransactionRepository(context);
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    [Fact]
    public async Task Add_WithValidTransaction_ShouldAddAndReturnTransaction()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
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
        result.Ticker.Should().Be("AAPL");
        result.SharesQuantity.Should().Be(10m);
        result.SharePrice.Should().Be(150m);

        var savedTransaction = await context.Transactions.FindAsync(transaction.Id);
        savedTransaction.Should().NotBeNull();
    }

    [Fact]
    public async Task Add_ShouldPersistAllProperties()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var date = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var transaction = new Transaction
        {
            Id = transactionId,
            Ticker = "GOOGL",
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
        savedTransaction!.Ticker.Should().Be("GOOGL");
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
        var userId = Guid.NewGuid();
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                Ticker = "AAPL",
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
                Ticker = "GOOGL",
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
                Ticker = "MSFT",
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
        result.Select(t => t.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "GOOGL", "MSFT" });
    }

    [Fact]
    public async Task AddBulk_WithValidTransactions_ShouldAddAllTransactions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactions = new List<Transaction?>
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                Ticker = "AAPL",
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
                Ticker = "GOOGL",
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
        var savedTransactions = await context.Transactions.ToListAsync();
        savedTransactions.Should().HaveCount(2);
        savedTransactions.Select(t => t.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "GOOGL" });
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
        var transactions = Enumerable.Range(1, 100)
            .Select(i => (Transaction?)new Transaction
            {
                Id = Guid.NewGuid(),
                Ticker = $"TICK{i}",
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
        var userId = Guid.NewGuid();
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                Ticker = "AAPL",
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
                Ticker = "GOOGL",
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
                Ticker = "MSFT",
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
        result.Select(t => t.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "MSFT" });
    }

    [Fact]
    public async Task GetOpenPositionsByUser_ShouldReturnOnlyTransactionsForSpecifiedUser()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                Ticker = "AAPL",
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
                Ticker = "GOOGL",
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
                Ticker = "MSFT",
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
        result.Select(t => t.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "MSFT" });
    }

    [Fact]
    public async Task GetOpenPositionsByUser_ShouldReturnTransactionsOrderedByDateDescending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldDate = new DateTime(2024, 1, 1);
        var middleDate = new DateTime(2024, 6, 1);
        var newDate = new DateTime(2025, 1, 1);
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                Ticker = "OLD",
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
                Ticker = "NEW",
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
                Ticker = "MIDDLE",
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
        result[0].Ticker.Should().Be("NEW");
        result[1].Ticker.Should().Be("MIDDLE");
        result[2].Ticker.Should().Be("OLD");
    }

    [Fact]
    public async Task GetOpenPositionsByUser_WithMixedTypesAndUsers_ShouldFilterCorrectly()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var transactions = new[]
        {
            // Target user's Buy transactions (should be included)
            new Transaction
            {
                Id = Guid.NewGuid(),
                Ticker = "AAPL",
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
                Ticker = "MSFT",
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
                Ticker = "GOOGL",
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
                Ticker = "TSLA",
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
        result.Select(t => t.Ticker).Should().BeEquivalentTo(new[] { "AAPL", "MSFT" });
    }

    [Fact]
    public async Task Add_WithComputedProperties_ShouldCalculateCorrectly()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
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
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                Ticker = "AAPL",
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
                Ticker = "GOOGL",
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
}

