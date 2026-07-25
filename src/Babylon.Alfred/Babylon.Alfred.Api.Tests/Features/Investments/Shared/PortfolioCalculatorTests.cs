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
        var fifoResult = PortfolioCalculator.Calculate(transactions);
        var totalShares = fifoResult.TotalShares;
        var costBasis = fifoResult.CostBasis;

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
        var fifoResult = PortfolioCalculator.Calculate(transactions);
        var totalShares = fifoResult.TotalShares;
        var costBasis = fifoResult.CostBasis;

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
        var fifoResult = PortfolioCalculator.Calculate(transactions);
        var totalShares = fifoResult.TotalShares;
        var costBasis = fifoResult.CostBasis;

        // Assert
        totalShares.Should().Be(0m);
        costBasis.Should().Be(0m);

        // Buy Cost = (10 * 100) + 5 = 1005. Tax of 2 is excluded from cost basis.
        // Sell Net Proceeds = (10 * 120) - 5 = 1195. Tax of 3 is excluded.
        // Realized PnL = 1195 - 1005 = 190
        // PnL % = 190 / 1005 * 100 = 18.9055...%

        var sellTransaction = transactions[1];
        sellTransaction.RealizedPnL.Should().Be(190m);
        sellTransaction.RealizedPnLPct.Should().BeApproximately(18.9055m, 0.0001m);
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
        var fifoResult = PortfolioCalculator.Calculate(transactions);
        var totalShares = fifoResult.TotalShares;
        var costBasis = fifoResult.CostBasis;

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
        var fifoResult = PortfolioCalculator.Calculate(transactions);
        var totalShares = fifoResult.TotalShares;
        var costBasis = fifoResult.CostBasis;

        // Assert
        totalShares.Should().Be(0m);
        costBasis.Should().Be(0m);

        var sellTransaction = transactions[1];
        sellTransaction.RealizedPnL.Should().Be(995m); // (10 * 100) - 5
        sellTransaction.RealizedPnLPct.Should().BeNull();
    }
    [Fact]
    public void CalculateCostBasis_BuyTransactionWithTax_ShouldNotIncludeTaxInCostBasis()
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
                Tax = 20m  // Tax must NOT be included in Buy cost basis
            }
        };

        // Act
        var fifoResult = PortfolioCalculator.Calculate(transactions);
        var totalShares = fifoResult.TotalShares;
        var costBasis = fifoResult.CostBasis;

        // Assert
        // Cost basis = (10 * 100) + 5 = 1005. Tax of 20 is excluded.
        totalShares.Should().Be(10m);
        costBasis.Should().Be(1005m);
    }

    [Fact]
    public void CalculateCostBasis_SellTransactionWithTax_ShouldNotDeductTaxFromProceeds()
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
                Fees = 5m
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 120m,
                Fees = 5m,
                Tax = 10m  // Tax must NOT be deducted from Sell proceeds
            }
        };

        // Act
        var fifoResult = PortfolioCalculator.Calculate(transactions);
        var totalShares = fifoResult.TotalShares;
        var costBasis = fifoResult.CostBasis;

        // Assert
        // Buy cost basis = (10 * 100) + 5 = 1005
        // Sell proceeds = (10 * 120) - 5 = 1195. Tax of 10 is excluded.
        // Realized PnL = 1195 - 1005 = 190
        totalShares.Should().Be(0m);
        costBasis.Should().Be(0m);
        transactions[1].RealizedPnL.Should().Be(190m);
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

    [Fact]
    public void Calculate_RealizedPnLByTransactionId_WithZeroCostBasis_ShouldStillCalculatePnL()
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
                SharePrice = 0m, // Free shares
                Fees = 0m,
                CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 5m,
                CreatedAt = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var results = PortfolioCalculator.Calculate(transactions).RealizedPnLByTransactionId;

        // Assert
        var sellResult = results[transactions[1].Id];
        sellResult.RealizedPnL.Should().Be(995m); // (10 * 100) - 5 - 0
        sellResult.RealizedPnLPct.Should().BeNull(); // Cannot calculate % of 0 cost
    }

    [Fact]
    public void Calculate_RealizedPnLByTransactionId_BuyTransactionWithTax_ShouldNotIncludeTaxInCostBasis()
    {
        // Arrange
        var buyId = Guid.NewGuid();
        var sellId = Guid.NewGuid();
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = buyId,
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 5m,
                Tax = 20m,  // Tax must NOT be included in Buy cost basis
                CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = sellId,
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 130m,
                Fees = 5m,
                Tax = 0m,  // Tax=0 to isolate the Buy Tax exclusion being tested
                CreatedAt = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var results = PortfolioCalculator.Calculate(transactions).RealizedPnLByTransactionId;

        // Assert
        // Buy cost basis = (10 * 100) + 5 = 1005. Tax of 20 excluded.
        // Sell proceeds = (10 * 130) - 5 = 1295
        // Realized PnL = 1295 - 1005 = 290
        // PnL % = 290 / 1005 * 100 = 28.8557...%
        var sellResult = results[sellId];
        sellResult.RealizedPnL.Should().Be(290m);
        sellResult.RealizedPnLPct.Should().BeApproximately(28.8557m, 0.0001m);
    }

    [Fact]
    public void Calculate_RealizedPnLByTransactionId_SellTransactionWithTax_ShouldNotDeductTaxFromProceeds()
    {
        // Arrange
        var buyId = Guid.NewGuid();
        var sellId = Guid.NewGuid();
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = buyId,
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 5m,
                CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = sellId,
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 120m,
                Fees = 5m,
                Tax = 10m,  // Tax must NOT be deducted from Sell proceeds
                CreatedAt = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var results = PortfolioCalculator.Calculate(transactions).RealizedPnLByTransactionId;

        // Assert
        // Buy cost basis = (10 * 100) + 5 = 1005
        // Sell proceeds = (10 * 120) - 5 = 1195. Tax of 10 excluded.
        // Realized PnL = 1195 - 1005 = 190
        // PnL % = 190 / 1005 * 100 = 18.9055...%
        var sellResult = results[sellId];
        sellResult.RealizedPnL.Should().Be(190m);
        sellResult.RealizedPnLPct.Should().BeApproximately(18.9055m, 0.0001m);
    }

    [Fact]
    public void Calculate_RealizedPnLByTransactionId_WithPartialSell_ShouldCalculateCorrectly()
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
                CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 5m,
                SharePrice = 150m,
                Fees = 10m,
                CreatedAt = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var results = PortfolioCalculator.Calculate(transactions).RealizedPnLByTransactionId;

        // Assert
        var sellResult = results[transactions[1].Id];
        sellResult.RealizedPnL.Should().Be(240m); // (5 * 150) - 10 - (5 * 100) = 750 - 10 - 500 = 240
        sellResult.RealizedPnLPct.Should().Be(48m); // 240 / 500 * 100 = 48%
    }
}
