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

    [Fact]
    public async Task ExecuteAsync_WithMultipleSellsAcrossMultipleLotsAndFees_ShouldCalculateTotalRealizedPnLViaFifo()
    {
        // Arrange — one security, three lots at different prices, two sells that each span
        // lot boundaries, fees on every leg. This is the scenario the simple single-lot tests
        // above don't exercise: the total must come from a real FIFO walk, not a shortcut.
        //
        // Lot A: 10 @ 10, fee 2  -> cost 102  (10.2/share)
        // Lot B: 10 @ 20, fee 2  -> cost 202  (20.2/share)
        // Lot C: 10 @ 30, fee 2  -> cost 302  (30.2/share)
        //
        // Sell 1: 15 @ 25, fee 5
        //   consume all of A (cost 102) + 5 from B (5 * 20.2 = 101) -> costConsumed = 203
        //   netProceeds = 15*25 - 5 = 370 -> PnL1 = 370 - 203 = 167
        //   Lot B remaining: 5 shares, cost 101
        //
        // Sell 2: 10 @ 35, fee 3
        //   consume remaining B (5, cost 101) + 5 from C (5 * 30.2 = 151) -> costConsumed = 252
        //   netProceeds = 10*35 - 3 = 347 -> PnL2 = 347 - 252 = 95
        //   Lot C remaining: 5 shares, cost 151
        //
        // Total RealizedPnL = 167 + 95 = 262
        // Total cost basis consumed = 203 + 252 = 455 -> RealizedPnLPercentage = 262/455*100
        var userId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            new() { Id = Guid.NewGuid(), SecurityId = securityId, TransactionType = TransactionType.Buy, Date = baseDate, CreatedAt = baseDate, UpdatedAt = baseDate, SharesQuantity = 10m, SharePrice = 10m, Fees = 2m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId, TransactionType = TransactionType.Buy, Date = baseDate.AddDays(1), CreatedAt = baseDate.AddDays(1), UpdatedAt = baseDate.AddDays(1), SharesQuantity = 10m, SharePrice = 20m, Fees = 2m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId, TransactionType = TransactionType.Buy, Date = baseDate.AddDays(2), CreatedAt = baseDate.AddDays(2), UpdatedAt = baseDate.AddDays(2), SharesQuantity = 10m, SharePrice = 30m, Fees = 2m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId, TransactionType = TransactionType.Sell, Date = baseDate.AddDays(3), CreatedAt = baseDate.AddDays(3), UpdatedAt = baseDate.AddDays(3), SharesQuantity = 15m, SharePrice = 25m, Fees = 5m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId, TransactionType = TransactionType.Sell, Date = baseDate.AddDays(4), CreatedAt = baseDate.AddDays(4), UpdatedAt = baseDate.AddDays(4), SharesQuantity = 10m, SharePrice = 35m, Fees = 3m, UserId = userId }
        };

        SetupPortfolioData(userId, transactions, [(securityId, "AAPL", 40m)]);
        var capturedSnapshot = CaptureAddedSnapshot();

        // Act
        await sut.ExecuteAsync();

        // Assert
        capturedSnapshot.Value.Should().NotBeNull();
        capturedSnapshot.Value!.RealizedPnL.Should().Be(262m);
        capturedSnapshot.Value!.RealizedPnLPercentage.Should().BeApproximately(262m / 455m * 100m, 0.0001m);
        capturedSnapshot.Value!.TotalInvested.Should().Be(151m); // remaining 5 shares of Lot C: 302 - 151
    }

    [Fact]
    public async Task ExecuteAsync_WithSplitBetweenSells_ShouldCalculateTotalRealizedPnLCorrectly()
    {
        // Arrange — split occurs between two rounds of buy/sell activity on the same security.
        // Buy 100 @ 10, fee 0 -> cost 1000
        // 2-for-1 split -> 200 shares, cost 1000 unchanged (5/share)
        // Sell 150 @ 8, fee 0 -> costConsumed = 150*5 = 750; netProceeds = 1200; PnL1 = 450
        //   Remaining: 50 shares, cost 250
        // Buy 50 @ 6, fee 0 -> new lot, cost 300 (6/share)
        //   Lots now: 50 @ 5 (cost 250), 50 @ 6 (cost 300)
        // Sell 60 @ 9, fee 0
        //   consume 50 from old lot (cost 250) + 10 from new lot (10*6=60) -> costConsumed = 310
        //   netProceeds = 540 -> PnL2 = 540 - 310 = 230
        // Total RealizedPnL = 450 + 230 = 680
        var userId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            new() { Id = Guid.NewGuid(), SecurityId = securityId, TransactionType = TransactionType.Buy, Date = baseDate, CreatedAt = baseDate, UpdatedAt = baseDate, SharesQuantity = 100m, SharePrice = 10m, Fees = 0m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId, TransactionType = TransactionType.Split, Date = baseDate.AddDays(1), CreatedAt = baseDate.AddDays(1), UpdatedAt = baseDate.AddDays(1), SharesQuantity = 2m, SharePrice = 0m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId, TransactionType = TransactionType.Sell, Date = baseDate.AddDays(2), CreatedAt = baseDate.AddDays(2), UpdatedAt = baseDate.AddDays(2), SharesQuantity = 150m, SharePrice = 8m, Fees = 0m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId, TransactionType = TransactionType.Buy, Date = baseDate.AddDays(3), CreatedAt = baseDate.AddDays(3), UpdatedAt = baseDate.AddDays(3), SharesQuantity = 50m, SharePrice = 6m, Fees = 0m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId, TransactionType = TransactionType.Sell, Date = baseDate.AddDays(4), CreatedAt = baseDate.AddDays(4), UpdatedAt = baseDate.AddDays(4), SharesQuantity = 60m, SharePrice = 9m, Fees = 0m, UserId = userId }
        };

        SetupPortfolioData(userId, transactions, [(securityId, "AAPL", 10m)]);
        var capturedSnapshot = CaptureAddedSnapshot();

        // Act
        await sut.ExecuteAsync();

        // Assert
        capturedSnapshot.Value.Should().NotBeNull();
        capturedSnapshot.Value!.RealizedPnL.Should().Be(680m);
        capturedSnapshot.Value!.TotalInvested.Should().Be(240m); // 40 shares left @ 6/share
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleSecuritiesEachRequiringFifoAcrossLots_ShouldSumTotalRealizedPnLCorrectly()
    {
        // Arrange — the "grand total" test: two securities, each requiring its own multi-lot
        // FIFO walk, summed into a single portfolio-level RealizedPnL. Proves the aggregation
        // in CreateSnapshotForUserAsync doesn't just work for simple single-lot cases.
        //
        // Security 1 (multi-lot, same math as the fees test above): RealizedPnL = 262, cost consumed = 455
        // Security 2 (single lot): Buy 100 @ 5, fee 0 -> cost 500
        //   Sell 50 @ 8, fee 0 -> costConsumed = 250; netProceeds = 400; PnL = 150
        //
        // Total RealizedPnL = 262 + 150 = 412
        // Total cost basis consumed = 455 + 250 = 705 -> RealizedPnLPercentage = 412/705*100
        var userId = Guid.NewGuid();
        var securityId1 = Guid.NewGuid();
        var securityId2 = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            // Security 1: three lots, two FIFO-spanning sells
            new() { Id = Guid.NewGuid(), SecurityId = securityId1, TransactionType = TransactionType.Buy, Date = baseDate, CreatedAt = baseDate, UpdatedAt = baseDate, SharesQuantity = 10m, SharePrice = 10m, Fees = 2m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId1, TransactionType = TransactionType.Buy, Date = baseDate.AddDays(1), CreatedAt = baseDate.AddDays(1), UpdatedAt = baseDate.AddDays(1), SharesQuantity = 10m, SharePrice = 20m, Fees = 2m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId1, TransactionType = TransactionType.Buy, Date = baseDate.AddDays(2), CreatedAt = baseDate.AddDays(2), UpdatedAt = baseDate.AddDays(2), SharesQuantity = 10m, SharePrice = 30m, Fees = 2m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId1, TransactionType = TransactionType.Sell, Date = baseDate.AddDays(3), CreatedAt = baseDate.AddDays(3), UpdatedAt = baseDate.AddDays(3), SharesQuantity = 15m, SharePrice = 25m, Fees = 5m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId1, TransactionType = TransactionType.Sell, Date = baseDate.AddDays(4), CreatedAt = baseDate.AddDays(4), UpdatedAt = baseDate.AddDays(4), SharesQuantity = 10m, SharePrice = 35m, Fees = 3m, UserId = userId },

            // Security 2: single lot
            new() { Id = Guid.NewGuid(), SecurityId = securityId2, TransactionType = TransactionType.Buy, Date = baseDate, CreatedAt = baseDate, UpdatedAt = baseDate, SharesQuantity = 100m, SharePrice = 5m, Fees = 0m, UserId = userId },
            new() { Id = Guid.NewGuid(), SecurityId = securityId2, TransactionType = TransactionType.Sell, Date = baseDate.AddDays(1), CreatedAt = baseDate.AddDays(1), UpdatedAt = baseDate.AddDays(1), SharesQuantity = 50m, SharePrice = 8m, Fees = 0m, UserId = userId }
        };

        SetupPortfolioData(userId, transactions, [(securityId1, "AAPL", 40m), (securityId2, "MSFT", 10m)]);
        var capturedSnapshot = CaptureAddedSnapshot();

        // Act
        await sut.ExecuteAsync();

        // Assert
        capturedSnapshot.Value.Should().NotBeNull();
        capturedSnapshot.Value!.RealizedPnL.Should().Be(412m);
        capturedSnapshot.Value!.RealizedPnLPercentage.Should().BeApproximately(412m / 705m * 100m, 0.0001m);
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
