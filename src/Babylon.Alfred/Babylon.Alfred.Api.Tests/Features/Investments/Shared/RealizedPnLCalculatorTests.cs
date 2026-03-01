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
