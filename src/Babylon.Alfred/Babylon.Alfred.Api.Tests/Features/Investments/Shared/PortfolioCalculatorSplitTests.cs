using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Shared;

public class PortfolioCalculatorSplitTests
{
    [Fact]
    public void CalculateCostBasis_WithSplitAfterPurchases_ShouldOnlyAffectSharesHeldBeforeSplit()
    {
        // Arrange: Buy shares before split, then buy more after split
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1),
                SharesQuantity = 100m,
                SharePrice = 150m,
                Fees = 5m,
                Tax = 0m
            },
            new()
            {
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 2, 1),
                SharesQuantity = 50m,
                SharePrice = 160m,
                Fees = 3m,
                Tax = 0m
            },
            // 2-for-1 split on March 1st (should affect the 150 shares held before this date)
            new()
            {
                TransactionType = TransactionType.Split,
                Date = new DateTime(2024, 3, 1),
                SharesQuantity = 2.0m, // 2-for-1 split
                SharePrice = 0m,
                Fees = 0m,
                Tax = 0m
            },
            // Buy more shares AFTER the split (should NOT be affected by split)
            new()
            {
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 4, 1),
                SharesQuantity = 20m,
                SharePrice = 75m, // Post-split price (half of original)
                Fees = 2m,
                Tax = 0m
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        // Before split: 100 + 50 = 150 shares
        // After split: 150 × 2 = 300 shares
        // After post-split purchase: 300 + 20 = 320 shares
        totalShares.Should().Be(320m);

        // Cost basis:
        // First buy: (100 × 150) + 5 = 15,005
        // Second buy: (50 × 160) + 3 = 8,003
        // Split: cost basis unchanged = 23,008
        // Post-split buy: (20 × 75) + 2 = 1,502
        // Total: 23,008 + 1,502 = 24,510
        costBasis.Should().Be(24_510m);

        // Average cost per share should be: 24,510 / 320 = 76.59375
        var averageSharePrice = costBasis / totalShares;
        averageSharePrice.Should().BeApproximately(76.59375m, 0.0001m);
    }

    [Fact]
    public void CalculateCostBasis_WithSplitBeforeAnyPurchases_ShouldBeIgnored()
    {
        // Arrange: Split happens before any purchases
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                TransactionType = TransactionType.Split,
                Date = new DateTime(2024, 1, 1),
                SharesQuantity = 2.0m,
                SharePrice = 0m,
                Fees = 0m,
                Tax = 0m
            },
            new()
            {
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 2, 1),
                SharesQuantity = 100m,
                SharePrice = 150m,
                Fees = 5m,
                Tax = 0m
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        // Split should be ignored (no shares to split)
        // Only the buy transaction should count
        totalShares.Should().Be(100m);
        costBasis.Should().Be(15_005m); // (100 × 150) + 5
    }

    [Fact]
    public void CalculateCostBasis_WithSplitAfterSellingAllShares_ShouldBeIgnored()
    {
        // Arrange: Buy, sell all, then split
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1),
                SharesQuantity = 100m,
                SharePrice = 150m,
                Fees = 5m,
                Tax = 0m
            },
            new()
            {
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 2, 1),
                SharesQuantity = 100m,
                SharePrice = 160m,
                Fees = 5m,
                Tax = 0m
            },
            new()
            {
                TransactionType = TransactionType.Split,
                Date = new DateTime(2024, 3, 1),
                SharesQuantity = 2.0m,
                SharePrice = 0m,
                Fees = 0m,
                Tax = 0m
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        // After selling all shares, split should be ignored (no shares to split)
        totalShares.Should().Be(0m);
        costBasis.Should().Be(0m);
    }

    [Fact]
    public void CalculateCostBasis_WithReverseSplit_ShouldReduceSharesCorrectly()
    {
        // Arrange: 1-for-2 reverse split (halves the shares)
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1),
                SharesQuantity = 200m,
                SharePrice = 50m,
                Fees = 10m,
                Tax = 0m
            },
            new()
            {
                TransactionType = TransactionType.Split,
                Date = new DateTime(2024, 2, 1),
                SharesQuantity = 0.5m, // 1-for-2 reverse split
                SharePrice = 0m,
                Fees = 0m,
                Tax = 0m
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        // 200 shares × 0.5 = 100 shares
        totalShares.Should().Be(100m);
        // Cost basis unchanged: (200 × 50) + 10 = 10,010
        costBasis.Should().Be(10_010m);
        // Average cost per share doubles: 10,010 / 100 = 100.10
        var averageSharePrice = costBasis / totalShares;
        averageSharePrice.Should().Be(100.10m);
    }

    [Fact]
    public void CalculateCostBasis_WithMultipleSplits_ShouldProcessChronologically()
    {
        // Arrange: Multiple splits over time
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1),
                SharesQuantity = 100m,
                SharePrice = 200m,
                Fees = 10m,
                Tax = 0m
            },
            // First split: 2-for-1
            new()
            {
                TransactionType = TransactionType.Split,
                Date = new DateTime(2024, 2, 1),
                SharesQuantity = 2.0m,
                SharePrice = 0m,
                Fees = 0m,
                Tax = 0m
            },
            // Second split: 3-for-1
            new()
            {
                TransactionType = TransactionType.Split,
                Date = new DateTime(2024, 3, 1),
                SharesQuantity = 3.0m,
                SharePrice = 0m,
                Fees = 0m,
                Tax = 0m
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        // After first split: 100 × 2 = 200 shares
        // After second split: 200 × 3 = 600 shares
        totalShares.Should().Be(600m);
        // Cost basis unchanged: (100 × 200) + 10 = 20,010
        costBasis.Should().Be(20_010m);
        // Average cost per share: 20,010 / 600 = 33.35
        var averageSharePrice = costBasis / totalShares;
        averageSharePrice.Should().BeApproximately(33.35m, 0.01m);
    }

    [Fact]
    public void CalculateCostBasis_WithSameDaySplitAndBuy_ShouldNotMultiplyPostSplitBuy()
    {
        // Arrange: Split and post-split buy on the same date.
        // The buy was entered into the system BEFORE the split (earlier CreatedAt).
        // The split should NOT multiply the same-day buy (splits take effect at market open).
        var splitDate = new DateTime(2024, 6, 7, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<PortfolioTransactionDto>
        {
            // Pre-split buy on an earlier date
            new()
            {
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 30m,
                Fees = 0m,
                Tax = 0m
            },
            // Post-split buy on the SAME DATE as the split, entered BEFORE the split in the system
            new()
            {
                TransactionType = TransactionType.Buy,
                Date = splitDate,
                CreatedAt = new DateTime(2024, 6, 7, 14, 0, 0, DateTimeKind.Utc), // entered first
                SharesQuantity = 10m,
                SharePrice = 10m, // post-split price
                Fees = 0m,
                Tax = 0m
            },
            // 3-for-1 split, entered AFTER the buy in the system
            new()
            {
                TransactionType = TransactionType.Split,
                Date = splitDate,
                CreatedAt = new DateTime(2024, 6, 7, 16, 0, 0, DateTimeKind.Utc), // entered second
                SharesQuantity = 3m,
                SharePrice = 0m,
                Fees = 0m,
                Tax = 0m
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        // Pre-split: 10 shares × 3 = 30 shares (from May 1 buy, correctly multiplied)
        // Post-split same-day buy: 10 shares (NOT multiplied - entered on split date at post-split price)
        // Total: 30 + 10 = 40 shares
        totalShares.Should().Be(40m);

        // Cost basis: (10 × 30) + (10 × 10) = 300 + 100 = 400
        costBasis.Should().Be(400m);
    }

    [Fact]
    public void CalculateCostBasis_WithSameDaySplitAndSell_ShouldSellPostSplitShares()
    {
        // Arrange: A sell on the same day as a split should sell post-split shares
        var splitDate = new DateTime(2024, 6, 7, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 100m,
                SharePrice = 30m,
                Fees = 0m,
                Tax = 0m
            },
            // 2-for-1 split
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Split,
                Date = splitDate,
                CreatedAt = new DateTime(2024, 6, 7, 14, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 2m,
                SharePrice = 0m,
                Fees = 0m,
                Tax = 0m
            },
            // Sell on the same day as the split - should sell post-split shares
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = splitDate,
                CreatedAt = new DateTime(2024, 6, 7, 10, 0, 0, DateTimeKind.Utc), // entered before split
                SharesQuantity = 50m,
                SharePrice = 15m, // post-split price
                Fees = 0m,
                Tax = 0m
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        // After split: 100 × 2 = 200 shares
        // After sell: 200 - 50 = 150 shares
        totalShares.Should().Be(150m);

        // Cost basis of 200 shares = 3000. Average = 15 per share.
        // Sell 50 shares: cost consumed = 50 × 15 = 750
        // Remaining cost basis = 3000 - 750 = 2250
        costBasis.Should().Be(2250m);
    }

    [Fact]
    public void CalculateCostBasis_WithSplitAndSell_ShouldCalculatePnLCorrectly()
    {
        // Arrange
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1),
                SharesQuantity = 100m,
                SharePrice = 100m,
                Fees = 0m,
                CreatedAt = new DateTime(2024, 1, 1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Split,
                Date = new DateTime(2024, 2, 1),
                SharesQuantity = 2.0m, // 2-for-1
                SharePrice = 0m,
                Fees = 0m,
                CreatedAt = new DateTime(2024, 2, 1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 3, 1),
                SharesQuantity = 100m, // Sell half of split shares
                SharePrice = 60m,
                Fees = 10m,
                CreatedAt = new DateTime(2024, 3, 1)
            }
        };

        // Act
        var (totalShares, costBasis) = PortfolioCalculator.CalculateCostBasis(transactions);

        // Assert
        // After split: 200 shares, 10,000 cost basis -> avg cost 50
        // Sell 100 shares at 60:
        // PnL = (100 * 60) - 10 - (100 * 50) = 6000 - 10 - 5000 = 990
        var sellTransaction = transactions[2];
        sellTransaction.RealizedPnL.Should().Be(990m);
        sellTransaction.RealizedPnLPct.Should().Be(19.8m); // 990 / 5000 * 100

        totalShares.Should().Be(100m);
        costBasis.Should().Be(5000m);
    }
}

