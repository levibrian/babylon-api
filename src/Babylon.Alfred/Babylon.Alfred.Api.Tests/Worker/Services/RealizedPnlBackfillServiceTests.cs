using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using Babylon.Alfred.Worker.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Worker.Services;

public class RealizedPnlBackfillServiceTests
{
    private readonly AutoMocker autoMocker = new();
    private readonly RealizedPnlBackfillService sut;

    public RealizedPnlBackfillServiceTests()
    {
        autoMocker.Use(Mock.Of<ILogger<RealizedPnlBackfillService>>());
        sut = autoMocker.CreateInstance<RealizedPnlBackfillService>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoUsersWithUnbackfilledSells_ShouldReturnWithoutCallingGetAllByUser()
    {
        // Arrange
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetDistinctUserIdsWithUnbackfilledSellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        await sut.ExecuteAsync();

        // Assert
        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.GetAllByUser(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleUnbackfilledSell_ShouldCalculateAndPersistPnL()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var buyId = Guid.NewGuid();
        var sellId = Guid.NewGuid();

        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = buyId,
                SecurityId = securityId,
                TransactionType = TransactionType.Buy,
                Date = baseDate,
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
                SharesQuantity = 100m,
                SharePrice = 10m,
                Fees = 0m,
                UserId = userId,
                RealizedPnL = null,
                RealizedPnLPct = null
            },
            new()
            {
                Id = sellId,
                SecurityId = securityId,
                TransactionType = TransactionType.Sell,
                Date = baseDate.AddDays(1),
                CreatedAt = baseDate.AddDays(1),
                UpdatedAt = baseDate.AddDays(1),
                SharesQuantity = 50m,
                SharePrice = 20m,
                Fees = 0m,
                UserId = userId,
                RealizedPnL = null,
                RealizedPnLPct = null
            }
        };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetDistinctUserIdsWithUnbackfilledSellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { userId });

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(userId))
            .ReturnsAsync(transactions);

        // Act
        await sut.ExecuteAsync();

        // Assert
        // Buy: 100 shares @ 10 + 0 fees = cost basis 1000
        // Sell: 50 shares @ 20 - 0 fees = proceeds 1000
        // Cost basis consumed = 50 * (1000 / 100) = 500
        // RealizedPnL = 1000 - 500 = 500
        // RealizedPnLPct = (500 / 500) * 100 = 100
        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.UpdateBulkAsync(
                It.Is<IList<Transaction>>(list =>
                    list.Count == 1 &&
                    list[0].Id == sellId &&
                    list[0].RealizedPnL == 500m &&
                    list[0].RealizedPnLPct == 100m),
                It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyBackfilledSell_ShouldSkipUpdateForThatTransaction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var securityId = Guid.NewGuid();

        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var alreadyBackfilledSellId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SecurityId = securityId,
                TransactionType = TransactionType.Buy,
                Date = baseDate,
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
                SharesQuantity = 100m,
                SharePrice = 10m,
                Fees = 0m,
                UserId = userId
            },
            new()
            {
                Id = alreadyBackfilledSellId,
                SecurityId = securityId,
                TransactionType = TransactionType.Sell,
                Date = baseDate.AddDays(1),
                CreatedAt = baseDate.AddDays(1),
                UpdatedAt = baseDate.AddDays(1),
                SharesQuantity = 50m,
                SharePrice = 20m,
                Fees = 0m,
                UserId = userId,
                RealizedPnL = 500m,     // Already calculated
                RealizedPnLPct = 100m
            }
        };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetDistinctUserIdsWithUnbackfilledSellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { userId });

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(userId))
            .ReturnsAsync(transactions);

        // Act
        await sut.ExecuteAsync();

        // Assert
        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.UpdateBulkAsync(It.IsAny<IList<Transaction>>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleUsers_ShouldProcessEachUserIndependently()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var buildUserTransactions = (Guid uid) => new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SecurityId = securityId,
                TransactionType = TransactionType.Buy,
                Date = baseDate,
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
                SharesQuantity = 100m,
                SharePrice = 10m,
                Fees = 0m,
                UserId = uid
            },
            new()
            {
                Id = Guid.NewGuid(),
                SecurityId = securityId,
                TransactionType = TransactionType.Sell,
                Date = baseDate.AddDays(1),
                CreatedAt = baseDate.AddDays(1),
                UpdatedAt = baseDate.AddDays(1),
                SharesQuantity = 50m,
                SharePrice = 20m,
                Fees = 0m,
                UserId = uid,
                RealizedPnL = null
            }
        };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetDistinctUserIdsWithUnbackfilledSellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { userId1, userId2 });

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(userId1))
            .ReturnsAsync(buildUserTransactions(userId1));

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(userId2))
            .ReturnsAsync(buildUserTransactions(userId2));

        // Act
        await sut.ExecuteAsync();

        // Assert
        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.GetAllByUser(userId1), Times.Once);

        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.GetAllByUser(userId2), Times.Once);

        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.UpdateBulkAsync(It.IsAny<IList<Transaction>>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasNoTransactions_ShouldNotCallUpdateBulk()
    {
        // Arrange
        var userId = Guid.NewGuid();

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetDistinctUserIdsWithUnbackfilledSellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { userId });

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(userId))
            .ReturnsAsync(new List<Transaction>());

        // Act
        await sut.ExecuteAsync();

        // Assert
        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.UpdateBulkAsync(It.IsAny<IList<Transaction>>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOneUserFails_ShouldContinueWithOtherUsers()
    {
        // Arrange
        var failingUserId = Guid.NewGuid();
        var successUserId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetDistinctUserIdsWithUnbackfilledSellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { failingUserId, successUserId });

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(failingUserId))
            .ThrowsAsync(new InvalidOperationException("Simulated database failure"));

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(successUserId))
            .ReturnsAsync(new List<Transaction>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SecurityId = securityId,
                    TransactionType = TransactionType.Buy,
                    Date = baseDate,
                    CreatedAt = baseDate,
                    UpdatedAt = baseDate,
                    SharesQuantity = 100m,
                    SharePrice = 10m,
                    Fees = 0m,
                    UserId = successUserId
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SecurityId = securityId,
                    TransactionType = TransactionType.Sell,
                    Date = baseDate.AddDays(1),
                    CreatedAt = baseDate.AddDays(1),
                    UpdatedAt = baseDate.AddDays(1),
                    SharesQuantity = 50m,
                    SharePrice = 20m,
                    Fees = 0m,
                    UserId = successUserId,
                    RealizedPnL = null
                }
            });

        // Act
        var act = async () => await sut.ExecuteAsync();

        // Assert — should not propagate the exception
        await act.Should().NotThrowAsync();

        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.GetAllByUser(successUserId), Times.Once);

        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.UpdateBulkAsync(It.IsAny<IList<Transaction>>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoBuyLotsForSell_ShouldNotUpdateTransaction()
    {
        // Arrange — sell with no preceding buy (orphaned sell)
        var userId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SecurityId = securityId,
                TransactionType = TransactionType.Sell,
                Date = baseDate,
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
                SharesQuantity = 10m,
                SharePrice = 150m,
                Fees = 0m,
                UserId = userId,
                RealizedPnL = null
            }
        };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetDistinctUserIdsWithUnbackfilledSellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { userId });

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(userId))
            .ReturnsAsync(transactions);

        // Act
        await sut.ExecuteAsync();

        // Assert — calculator returns (null, null) for sell with no buy lots; nothing to persist
        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.UpdateBulkAsync(It.IsAny<IList<Transaction>>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleSecurities_ShouldProcessEachSecurityIndependently()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var securityId1 = Guid.NewGuid();
        var securityId2 = Guid.NewGuid();
        var sellId1 = Guid.NewGuid();
        var sellId2 = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            // Security 1: Buy then Sell
            new()
            {
                Id = Guid.NewGuid(),
                SecurityId = securityId1,
                TransactionType = TransactionType.Buy,
                Date = baseDate,
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
                SharesQuantity = 100m,
                SharePrice = 10m,
                Fees = 0m,
                UserId = userId
            },
            new()
            {
                Id = sellId1,
                SecurityId = securityId1,
                TransactionType = TransactionType.Sell,
                Date = baseDate.AddDays(1),
                CreatedAt = baseDate.AddDays(1),
                UpdatedAt = baseDate.AddDays(1),
                SharesQuantity = 50m,
                SharePrice = 20m,
                Fees = 0m,
                UserId = userId,
                RealizedPnL = null
            },
            // Security 2: Buy then Sell
            new()
            {
                Id = Guid.NewGuid(),
                SecurityId = securityId2,
                TransactionType = TransactionType.Buy,
                Date = baseDate,
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
                SharesQuantity = 200m,
                SharePrice = 5m,
                Fees = 0m,
                UserId = userId
            },
            new()
            {
                Id = sellId2,
                SecurityId = securityId2,
                TransactionType = TransactionType.Sell,
                Date = baseDate.AddDays(1),
                CreatedAt = baseDate.AddDays(1),
                UpdatedAt = baseDate.AddDays(1),
                SharesQuantity = 100m,
                SharePrice = 8m,
                Fees = 0m,
                UserId = userId,
                RealizedPnL = null
            }
        };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetDistinctUserIdsWithUnbackfilledSellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { userId });

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(userId))
            .ReturnsAsync(transactions);

        // Act
        await sut.ExecuteAsync();

        // Assert — both securities' sells are updated in a single UpdateBulkAsync call for this user
        // Security 1: cost basis consumed = 50 * (1000/100) = 500; proceeds = 50*20 = 1000; PnL = 500; PnLPct = 100
        // Security 2: cost basis consumed = 100 * (1000/200) = 500; proceeds = 100*8 = 800; PnL = 300; PnLPct = 60
        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.UpdateBulkAsync(
                It.Is<IList<Transaction>>(list =>
                    list.Count == 2 &&
                    list.Any(t => t.Id == sellId1 && t.RealizedPnL == 500m) &&
                    list.Any(t => t.Id == sellId2 && t.RealizedPnL == 300m)),
                It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_ShouldStopProcessingSubsequentUsers()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        using var cts = new CancellationTokenSource();

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetDistinctUserIdsWithUnbackfilledSellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { userId1, userId2 });

        // Cancel the token when userId1 is processed
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(userId1))
            .ReturnsAsync(new List<Transaction>())
            .Callback(cts.Cancel);

        // Act
        await sut.ExecuteAsync(cts.Token);

        // Assert
        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.GetAllByUser(userId1), Times.Once);

        autoMocker.GetMock<ITransactionRepository>()
            .Verify(r => r.GetAllByUser(userId2), Times.Never);
    }
}
