using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using Babylon.Alfred.Worker.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Worker.Services;

public class PortfolioSnapshotServiceTests
{
    private readonly AutoMocker autoMocker = new();
    private readonly PortfolioSnapshotService sut;

    public PortfolioSnapshotServiceTests()
    {
        autoMocker.Use(Mock.Of<ILogger<PortfolioSnapshotService>>());
        sut = autoMocker.CreateInstance<PortfolioSnapshotService>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasOnlyBuys_ShouldCreateSnapshotWithZeroRealizedPnL()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

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
            }
        };

        SetupPortfolioData(userId, transactions, [(securityId, "AAPL", 15m)]);
        var capturedSnapshot = CaptureAddedSnapshot();

        // Act
        await sut.ExecuteAsync();

        // Assert
        capturedSnapshot.Value.Should().NotBeNull();
        capturedSnapshot.Value!.RealizedPnL.Should().Be(0m);
        capturedSnapshot.Value!.RealizedPnLPercentage.Should().Be(0m);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasOneSell_ShouldCalculateRealizedPnLCorrectly()
    {
        // Arrange
        // Buy: 100 shares @ $10 = cost basis $1000
        // Sell: 50 shares @ $20 → proceeds $1000, cost basis consumed $500
        // RealizedPnL = $1000 - $500 = $500
        // RealizedPnLPercentage = ($500 / $500) * 100 = 100%
        var userId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

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
                Id = Guid.NewGuid(),
                SecurityId = securityId,
                TransactionType = TransactionType.Sell,
                Date = baseDate.AddDays(1),
                CreatedAt = baseDate.AddDays(1),
                UpdatedAt = baseDate.AddDays(1),
                SharesQuantity = 50m,
                SharePrice = 20m,
                Fees = 0m,
                UserId = userId
            }
        };

        SetupPortfolioData(userId, transactions, [(securityId, "AAPL", 25m)]);
        var capturedSnapshot = CaptureAddedSnapshot();

        // Act
        await sut.ExecuteAsync();

        // Assert
        capturedSnapshot.Value.Should().NotBeNull();
        capturedSnapshot.Value!.RealizedPnL.Should().Be(500m);
        capturedSnapshot.Value!.RealizedPnLPercentage.Should().Be(100.0000m);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasMultipleSellsAcrossSecurities_ShouldSumRealizedPnLAcrossSecurities()
    {
        // Arrange
        // Security 1: Buy 100 @ $10 = $1000 basis; Sell 50 @ $20 → RealizedPnL = $500, costConsumed = $500
        // Security 2: Buy 200 @ $5 = $1000 basis; Sell 100 @ $8 → RealizedPnL = $300, costConsumed = $500
        // Total RealizedPnL = $800
        // Total cost basis consumed = $1000
        // RealizedPnLPercentage = (800 / 1000) * 100 = 80%
        var userId = Guid.NewGuid();
        var securityId1 = Guid.NewGuid();
        var securityId2 = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(), SecurityId = securityId1, TransactionType = TransactionType.Buy,
                Date = baseDate, CreatedAt = baseDate, UpdatedAt = baseDate,
                SharesQuantity = 100m, SharePrice = 10m, Fees = 0m, UserId = userId
            },
            new()
            {
                Id = Guid.NewGuid(), SecurityId = securityId1, TransactionType = TransactionType.Sell,
                Date = baseDate.AddDays(1), CreatedAt = baseDate.AddDays(1), UpdatedAt = baseDate.AddDays(1),
                SharesQuantity = 50m, SharePrice = 20m, Fees = 0m, UserId = userId
            },
            new()
            {
                Id = Guid.NewGuid(), SecurityId = securityId2, TransactionType = TransactionType.Buy,
                Date = baseDate, CreatedAt = baseDate, UpdatedAt = baseDate,
                SharesQuantity = 200m, SharePrice = 5m, Fees = 0m, UserId = userId
            },
            new()
            {
                Id = Guid.NewGuid(), SecurityId = securityId2, TransactionType = TransactionType.Sell,
                Date = baseDate.AddDays(1), CreatedAt = baseDate.AddDays(1), UpdatedAt = baseDate.AddDays(1),
                SharesQuantity = 100m, SharePrice = 8m, Fees = 0m, UserId = userId
            }
        };

        SetupPortfolioData(userId, transactions, [(securityId1, "AAPL", 25m), (securityId2, "MSFT", 6m)]);
        var capturedSnapshot = CaptureAddedSnapshot();

        // Act
        await sut.ExecuteAsync();

        // Assert
        capturedSnapshot.Value.Should().NotBeNull();
        capturedSnapshot.Value!.RealizedPnL.Should().Be(800m);
        capturedSnapshot.Value!.RealizedPnLPercentage.Should().Be(80.0000m);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasMixOfProfitableAndLossSells_ShouldNetRealizedPnLCorrectly()
    {
        // Arrange
        // Security 1: Buy 100 @ $10; Sell 50 @ $20 → RealizedPnL = +$500, costConsumed = $500
        // Security 2: Buy 100 @ $20; Sell 50 @ $10 → RealizedPnL = -$500, costConsumed = $1000
        // Net RealizedPnL = $0
        // RealizedPnLPercentage = (0 / 1500) * 100 = 0%
        var userId = Guid.NewGuid();
        var securityId1 = Guid.NewGuid();
        var securityId2 = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(), SecurityId = securityId1, TransactionType = TransactionType.Buy,
                Date = baseDate, CreatedAt = baseDate, UpdatedAt = baseDate,
                SharesQuantity = 100m, SharePrice = 10m, Fees = 0m, UserId = userId
            },
            new()
            {
                Id = Guid.NewGuid(), SecurityId = securityId1, TransactionType = TransactionType.Sell,
                Date = baseDate.AddDays(1), CreatedAt = baseDate.AddDays(1), UpdatedAt = baseDate.AddDays(1),
                SharesQuantity = 50m, SharePrice = 20m, Fees = 0m, UserId = userId
            },
            new()
            {
                Id = Guid.NewGuid(), SecurityId = securityId2, TransactionType = TransactionType.Buy,
                Date = baseDate, CreatedAt = baseDate, UpdatedAt = baseDate,
                SharesQuantity = 100m, SharePrice = 20m, Fees = 0m, UserId = userId
            },
            new()
            {
                Id = Guid.NewGuid(), SecurityId = securityId2, TransactionType = TransactionType.Sell,
                Date = baseDate.AddDays(1), CreatedAt = baseDate.AddDays(1), UpdatedAt = baseDate.AddDays(1),
                SharesQuantity = 50m, SharePrice = 10m, Fees = 0m, UserId = userId
            }
        };

        SetupPortfolioData(userId, transactions, [(securityId1, "AAPL", 25m), (securityId2, "MSFT", 15m)]);
        var capturedSnapshot = CaptureAddedSnapshot();

        // Act
        await sut.ExecuteAsync();

        // Assert
        capturedSnapshot.Value.Should().NotBeNull();
        capturedSnapshot.Value!.RealizedPnL.Should().Be(0m);
        capturedSnapshot.Value!.RealizedPnLPercentage.Should().Be(0m);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoSellTransactionsExist_ShouldSetRealizedPnLPercentageToZero()
    {
        // Arrange — guard against divide-by-zero when no cost basis has been consumed
        var userId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

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
                SharesQuantity = 50m,
                SharePrice = 100m,
                Fees = 0m,
                UserId = userId
            }
        };

        SetupPortfolioData(userId, transactions, [(securityId, "TSLA", 120m)]);
        var capturedSnapshot = CaptureAddedSnapshot();

        // Act
        await sut.ExecuteAsync();

        // Assert
        capturedSnapshot.Value.Should().NotBeNull();
        capturedSnapshot.Value!.RealizedPnLPercentage.Should().Be(0m);
    }

    /// <summary>
    /// Returns a container whose Value will be set to the snapshot passed to AddSnapshotAsync.
    /// Uses a wrapper class to allow lambda capture.
    /// </summary>
    private SnapshotCapture CaptureAddedSnapshot()
    {
        var capture = new SnapshotCapture();
        autoMocker.GetMock<IPortfolioSnapshotRepository>()
            .Setup(r => r.AddSnapshotAsync(It.IsAny<PortfolioSnapshot>()))
            .Callback<PortfolioSnapshot>(s => capture.Value = s)
            .Returns(Task.CompletedTask);
        return capture;
    }

    private void SetupPortfolioData(
        Guid userId,
        List<Transaction> transactions,
        (Guid securityId, string ticker, decimal price)[] securities)
    {
        var securityEntities = securities
            .Select(s => new Security { Id = s.securityId, Ticker = s.ticker, SecurityName = s.ticker })
            .ToList();

        var marketPrices = securities
            .ToDictionary(s => s.ticker, s => new MarketPrice { Price = s.price });

        autoMocker.GetMock<IPortfolioSnapshotRepository>()
            .Setup(r => r.GetUserIdsWithPortfoliosAsync())
            .ReturnsAsync(new List<Guid> { userId });

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(r => r.GetAllByUser(userId))
            .ReturnsAsync(transactions);

        autoMocker.GetMock<ICashBalanceRepository>()
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((CashBalance?)null);

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(securityEntities);

        autoMocker.GetMock<IMarketPriceRepository>()
            .Setup(r => r.GetByTickersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(marketPrices);
    }

    private sealed class SnapshotCapture
    {
        public PortfolioSnapshot? Value { get; set; }
    }
}
