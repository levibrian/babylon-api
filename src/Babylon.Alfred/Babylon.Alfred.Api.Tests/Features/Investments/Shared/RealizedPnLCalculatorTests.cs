using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Shared;

public class RealizedPnLCalculatorTests
{
    [Fact]
    public void CalculateRealizedPnLByTransactionId_WithZeroCostBasis_ShouldStillCalculatePnL()
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
        var results = RealizedPnLCalculator.CalculateRealizedPnLByTransactionId(transactions);

        // Assert
        var sellResult = results[transactions[1].Id];
        sellResult.RealizedPnL.Should().Be(995m); // (10 * 100) - 5 - 0
        sellResult.RealizedPnLPct.Should().BeNull(); // Cannot calculate % of 0 cost
    }

    [Fact]
    public void CalculateRealizedPnLByTransactionId_BuyTransactionWithTax_ShouldNotIncludeTaxInCostBasis()
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
        var results = RealizedPnLCalculator.CalculateRealizedPnLByTransactionId(transactions);

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
    public void CalculateRealizedPnLByTransactionId_SellTransactionWithTax_ShouldNotDeductTaxFromProceeds()
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
        var results = RealizedPnLCalculator.CalculateRealizedPnLByTransactionId(transactions);

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
    public void CalculateRealizedPnLByTransactionId_WithPartialSell_ShouldCalculateCorrectly()
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
        var results = RealizedPnLCalculator.CalculateRealizedPnLByTransactionId(transactions);

        // Assert
        var sellResult = results[transactions[1].Id];
        sellResult.RealizedPnL.Should().Be(240m); // (5 * 150) - 10 - (5 * 100) = 750 - 10 - 500 = 240
        sellResult.RealizedPnLPct.Should().Be(48m); // 240 / 500 * 100 = 48%
    }
}
