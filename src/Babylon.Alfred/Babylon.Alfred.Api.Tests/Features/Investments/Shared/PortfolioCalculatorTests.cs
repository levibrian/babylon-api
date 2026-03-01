using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Shared;

public class PortfolioCalculatorTests
{
    [Fact]
    public void CalculateCostBasis_WithBuyAndSell_ShouldCalculateRealizedProfitLossCorrectly()
    {
        // Arrange
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 150m,
                Fees = 5m,
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 5m,
                SharePrice = 170m,
                Fees = 5m
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        totalShares.Should().Be(5m);
        costBasis.Should().Be(752.5m); // 1505 / 2

        var sellTransaction = transactions[1];
        sellTransaction.RealizedPnL.Should().Be(92.5m); // 845 - 752.5
        sellTransaction.RealizedPnLPct.Should().BeApproximately(12.2923588m, 0.0001m);
    }

    [Fact]
    public void CalculateCostBasis_UserScenario_ShouldCalculateCorrectly()
    {
        // Arrange
        // buy 10 shares at 10 euros per share
        // buy 10 shares at 10 euros per share
        // total position: 20 shares at 10 euros per share average
        // sell 5 shares at 12 euros per share
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 10m,
                Fees = 0m,
                CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 11, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 10m,
                Fees = 0m,
                CreatedAt = new DateTime(2024, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 5m,
                SharePrice = 12m,
                Fees = 0m,
                CreatedAt = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        totalShares.Should().Be(15m);
        costBasis.Should().Be(150m);

        var sellTransaction = transactions[2];
        sellTransaction.RealizedPnL.Should().Be(10m); // (5 * 12) - (5 * 10) = 60 - 50 = 10
        sellTransaction.RealizedPnLPct.Should().Be(20m); // 10 / 50 * 100 = 20%
    }

    [Fact]
    public void CalculateCostBasis_WithFeesAndTax_ShouldCalculatePnLCorrectly()
    {
        // Arrange
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 5m,
                Tax = 2m
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 120m,
                Fees = 5m,
                Tax = 3m
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        totalShares.Should().Be(0m);
        costBasis.Should().Be(0m);

        // Buy Cost = 1000 + 5 + 2 = 1007
        // Sell Net Proceeds = 1200 - 5 - 3 = 1192
        // Realized PnL = 1192 - 1007 = 185
        // PnL % = 185 / 1007 * 100 = 18.3714...%

        var sellTransaction = transactions[1];
        sellTransaction.RealizedPnL.Should().Be(185m);
        sellTransaction.RealizedPnLPct.Should().BeApproximately(18.3714m, 0.0001m);
    }

    [Fact]
    public void CalculateCostBasis_WhenSellingMoreThanOwned_ShouldIgnoreExcessShares()
    {
        // Arrange
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 0m,
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 15m,
                SharePrice = 120m,
                Fees = 0m,
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        totalShares.Should().Be(0m);
        costBasis.Should().Be(0m);

        var sellTransaction = transactions[1];
        sellTransaction.RealizedPnL.Should().Be(200m); // (10 * 120) - (10 * 100)
        sellTransaction.RealizedPnLPct.Should().Be(20m);
    }

    [Fact]
    public void CalculateCostBasis_WithZeroCostBasis_ShouldStillCalculatePnL()
    {
        // Arrange
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 0m,
                Fees = 0m,
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 5m
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        totalShares.Should().Be(0m);
        costBasis.Should().Be(0m);

        var sellTransaction = transactions[1];
        sellTransaction.RealizedPnL.Should().Be(995m); // (10 * 100) - 5
        sellTransaction.RealizedPnLPct.Should().BeNull();
    }
    [Theory]
    [InlineData(100, 1000, 10)]
    [InlineData(0, 1000, 0)]
    [InlineData(500, 0, 0)]
    [InlineData(250, 1000, 25)]
    public void CalculateCurrentAllocationPercentage_ShouldCalculateCorrectly(decimal marketValue, decimal totalValue, decimal expected)
    {
        // Act
        var result = PortfolioCalculator.CalculateCurrentAllocationPercentage(marketValue, totalValue);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(100, 10, 1000, 0)]
    [InlineData(50, 10, 1000, 50)]
    [InlineData(150, 10, 1000, -50)]
    public void CalculateRebalancingAmount_ShouldCalculateCorrectly(decimal currentMarketValue, decimal targetPercentage, decimal totalPortfolioValue, decimal expected)
    {
        // Act
        var result = PortfolioCalculator.CalculateRebalancingAmount(currentMarketValue, targetPercentage, totalPortfolioValue);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10.2, 10.0, RebalancingStatus.Balanced)]
    [InlineData(9.8, 10.0, RebalancingStatus.Balanced)]
    [InlineData(10.6, 10.0, RebalancingStatus.Overweight)]
    [InlineData(9.4, 10.0, RebalancingStatus.Underweight)]
    public void DetermineRebalancingStatus_ShouldDetermineCorrectly(decimal current, decimal target, RebalancingStatus expected)
    {
        // Act
        var result = PortfolioCalculator.DetermineRebalancingStatus(current, target);

        // Assert
        result.Should().Be(expected);
    }
}
