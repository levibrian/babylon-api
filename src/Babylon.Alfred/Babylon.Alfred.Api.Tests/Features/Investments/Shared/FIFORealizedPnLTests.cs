using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Shared;

public class FIFORealizedPnLTests
{
    [Fact]
    public void CalculateRealizedPnLByTransactionId_WithFIFOScenario_ShouldCalculateCorrectly()
    {
        // Arrange
        // 1. Buy 10 @ 100, Fees 10. Total Cost 1010. Cost per share 101.
        // 2. Buy 10 @ 200, Fees 20. Total Cost 2020. Cost per share 202.
        // 3. Sell 15 @ 250, Fees 30.
        // FIFO Cost Basis: (10 * 101) + (5 * 202) = 1010 + 1010 = 2020.
        // Net Proceeds: (15 * 250) - 30 = 3750 - 30 = 3720.
        // Realized PnL: 3720 - 2020 = 1700.

        // Weighted Average comparison (for info):
        // Total shares 20, Total cost 3030. Avg cost 151.5.
        // Sold cost: 15 * 151.5 = 2272.5.
        // Realized PnL: 3720 - 2272.5 = 1447.5.

        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 10m,
                CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 200m,
                Fees = 20m,
                CreatedAt = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 3, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 15m,
                SharePrice = 250m,
                Fees = 30m,
                CreatedAt = new DateTime(2024, 1, 3, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var results = RealizedPnLCalculator.CalculateRealizedPnLByTransactionId(transactions);

        // Assert
        var sellResult = results[transactions[2].Id];
        sellResult.RealizedPnL.Should().Be(1700m);
    }

    [Fact]
    public void CalculateCostBasis_WithFIFOScenario_ShouldReportCorrectRemainingCostBasis()
    {
        // Arrange (Same as above)
        // Remaining: 5 shares from Buy 2.
        // FIFO Cost Basis: 5 * 202 = 1010.

        // Weighted Average comparison (for info):
        // Remaining cost: 5 * 151.5 = 757.5.

        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 10m,
                CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 200m,
                Fees = 20m,
                CreatedAt = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 3, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 15m,
                SharePrice = 250m,
                Fees = 30m,
                CreatedAt = new DateTime(2024, 1, 3, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        totalShares.Should().Be(5m);
        costBasis.Should().Be(1010m);
    }

    [Fact]
    public void CalculateRealizedPnLByTransactionId_WithSplitAndFIFO_ShouldCalculateCorrectly()
    {
        // Arrange
        // 1. Buy 10 @ 100. Cost 1000.
        // 2. Buy 10 @ 200. Cost 2000.
        // 3. Split 2-for-1.
        //    Lot 1 becomes 20 shares @ 50.
        //    Lot 2 becomes 20 shares @ 100.
        // 4. Sell 25 @ 150.
        //    Use all 20 from Lot 1: cost = 1000.
        //    Use 5 from Lot 2: cost = 5 * 100 = 500.
        //    Total Cost Basis: 1500.
        //    Net Proceeds: 25 * 150 = 3750.
        //    Realized PnL: 3750 - 1500 = 2250.

        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1),
                SharesQuantity = 10m,
                SharePrice = 100m,
                CreatedAt = new DateTime(2024, 1, 1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 2),
                SharesQuantity = 10m,
                SharePrice = 200m,
                CreatedAt = new DateTime(2024, 1, 2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Split,
                Date = new DateTime(2024, 1, 3),
                SharesQuantity = 2.0m,
                CreatedAt = new DateTime(2024, 1, 3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 4),
                SharesQuantity = 25m,
                SharePrice = 150m,
                CreatedAt = new DateTime(2024, 1, 4)
            }
        };

        // Act
        var results = RealizedPnLCalculator.CalculateRealizedPnLByTransactionId(transactions);

        // Assert
        var sellResult = results[transactions[3].Id];
        sellResult.RealizedPnL.Should().Be(2250m);
    }
}
