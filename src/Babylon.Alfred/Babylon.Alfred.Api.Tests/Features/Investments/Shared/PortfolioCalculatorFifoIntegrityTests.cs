using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Shared;

/// <summary>
/// Correctness-focused tests for the unified <see cref="PortfolioCalculator.Calculate"/>.
/// This is the single source of truth for FIFO cost basis and realized PnL across the API and
/// Worker — errors here are money errors. These tests target invariants and edge cases that
/// individual scenario tests (<see cref="PortfolioCalculatorTests"/>, <see cref="PortfolioCalculatorSplitTests"/>)
/// don't cover: empty input, average share price, dividend handling, DTO/dictionary consistency,
/// and money conservation across the whole FIFO walk.
/// </summary>
public class PortfolioCalculatorFifoIntegrityTests
{
    [Fact]
    public void Calculate_WithEmptyTransactionList_ShouldReturnAllZerosAndEmptyDictionary()
    {
        // Act
        var result = PortfolioCalculator.Calculate(new List<PortfolioTransactionDto>());

        // Assert
        result.TotalShares.Should().Be(0m);
        result.AverageSharePrice.Should().Be(0m);
        result.CostBasis.Should().Be(0m);
        result.RealizedPnLByTransactionId.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_AverageSharePrice_WithSingleBuy_ShouldIncludeFeesPerShare()
    {
        // Arrange: 10 shares @ 100, fee 5 -> cost basis 1005 -> avg 100.5
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 5m
            }
        };

        // Act
        var result = PortfolioCalculator.Calculate(transactions);

        // Assert
        result.TotalShares.Should().Be(10m);
        result.CostBasis.Should().Be(1005m);
        result.AverageSharePrice.Should().Be(100.5m);
    }

    [Fact]
    public void Calculate_AverageSharePrice_WithMultipleBuysAtDifferentPrices_ShouldBeCostWeighted()
    {
        // Arrange: 10 @ 10 (cost 100) + 10 @ 20 (cost 200) = 20 shares, 300 cost -> avg 15
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 10m,
                Fees = 0m
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 20m,
                Fees = 0m
            }
        };

        // Act
        var result = PortfolioCalculator.Calculate(transactions);

        // Assert
        result.TotalShares.Should().Be(20m);
        result.CostBasis.Should().Be(300m);
        result.AverageSharePrice.Should().Be(15m);
    }

    [Fact]
    public void Calculate_AverageSharePrice_WhenAllSharesSold_ShouldBeZeroNotDivideByZero()
    {
        // Arrange
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 10m,
                Fees = 0m
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 12m,
                Fees = 0m
            }
        };

        // Act
        var result = PortfolioCalculator.Calculate(transactions);

        // Assert
        result.TotalShares.Should().Be(0m);
        result.CostBasis.Should().Be(0m);
        result.AverageSharePrice.Should().Be(0m);
    }

    [Fact]
    public void Calculate_WithZeroQuantitySell_ShouldBeNoOpAndReportNullPnL()
    {
        // Arrange — defensive case: a sell with SharesQuantity = 0 should not touch any lot
        var sellId = Guid.NewGuid();
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 0m
            },
            new()
            {
                Id = sellId,
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 0m,
                SharePrice = 150m,
                Fees = 0m
            }
        };

        // Act
        var result = PortfolioCalculator.Calculate(transactions);

        // Assert
        result.TotalShares.Should().Be(10m);
        result.CostBasis.Should().Be(1000m);
        result.RealizedPnLByTransactionId[sellId].RealizedPnL.Should().BeNull();
        result.RealizedPnLByTransactionId[sellId].RealizedPnLPct.Should().BeNull();
    }

    [Fact]
    public void Calculate_DividendTransactions_ShouldNotAffectSharesOrCostBasisAndReportNullPnL()
    {
        // Arrange: Buy, Dividend, Sell — dividend must be a complete no-op on FIFO state
        var dividendId = Guid.NewGuid();
        var transactions = new List<PortfolioTransactionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Buy,
                Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 100m,
                Fees = 0m
            },
            new()
            {
                Id = dividendId,
                TransactionType = TransactionType.Dividend,
                Date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 0m,
                SharePrice = 2.5m,
                Fees = 0m
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionType = TransactionType.Sell,
                Date = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                SharesQuantity = 10m,
                SharePrice = 120m,
                Fees = 0m
            }
        };

        // Act
        var result = PortfolioCalculator.Calculate(transactions);

        // Assert
        result.TotalShares.Should().Be(0m);
        result.CostBasis.Should().Be(0m);
        result.RealizedPnLByTransactionId[dividendId].RealizedPnL.Should().BeNull();
        result.RealizedPnLByTransactionId[dividendId].RealizedPnLPct.Should().BeNull();
    }

    [Fact]
    public void Calculate_RealizedPnLByTransactionId_ShouldContainEntryForEveryTransaction()
    {
        // Arrange: one of each transaction type
        var buyId = Guid.NewGuid();
        var dividendId = Guid.NewGuid();
        var splitId = Guid.NewGuid();
        var sellId = Guid.NewGuid();

        var transactions = new List<PortfolioTransactionDto>
        {
            new() { Id = buyId, TransactionType = TransactionType.Buy, Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 10m, SharePrice = 100m, Fees = 0m },
            new() { Id = dividendId, TransactionType = TransactionType.Dividend, Date = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 0m, SharePrice = 1m, Fees = 0m },
            new() { Id = splitId, TransactionType = TransactionType.Split, Date = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 2m, SharePrice = 0m, Fees = 0m },
            new() { Id = sellId, TransactionType = TransactionType.Sell, Date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 5m, SharePrice = 60m, Fees = 0m }
        };

        // Act
        var result = PortfolioCalculator.Calculate(transactions);

        // Assert — every transaction id must have an entry; non-sells are (null, null)
        result.RealizedPnLByTransactionId.Should().ContainKey(buyId);
        result.RealizedPnLByTransactionId.Should().ContainKey(dividendId);
        result.RealizedPnLByTransactionId.Should().ContainKey(splitId);
        result.RealizedPnLByTransactionId.Should().ContainKey(sellId);

        result.RealizedPnLByTransactionId[buyId].Should().Be((null, null));
        result.RealizedPnLByTransactionId[dividendId].Should().Be((null, null));
        result.RealizedPnLByTransactionId[splitId].Should().Be((null, null));
        result.RealizedPnLByTransactionId[sellId].RealizedPnL.Should().NotBeNull();
    }

    [Fact]
    public void Calculate_ForEverySell_DtoMutationAndDictionaryValue_ShouldBeIdenticalNotJustEqual()
    {
        // Arrange — regression guard for the FIFO unification: the DTO's RealizedPnL/RealizedPnLPct
        // (mutated in-place) and the returned RealizedPnLByTransactionId dictionary must never
        // diverge, since both are derived from the same single pass. Multiple sells across
        // multiple lots to exercise more than one code path through ProcessSellTransactionFIFO.
        var sell1Id = Guid.NewGuid();
        var sell2Id = Guid.NewGuid();

        var transactions = new List<PortfolioTransactionDto>
        {
            new() { Id = Guid.NewGuid(), TransactionType = TransactionType.Buy, Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 10m, SharePrice = 100m, Fees = 10m },
            new() { Id = Guid.NewGuid(), TransactionType = TransactionType.Buy, Date = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 10m, SharePrice = 200m, Fees = 20m },
            new() { Id = sell1Id, TransactionType = TransactionType.Sell, Date = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 15m, SharePrice = 250m, Fees = 30m },
            new() { Id = sell2Id, TransactionType = TransactionType.Sell, Date = new DateTime(2024, 1, 4, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 5m, SharePrice = 300m, Fees = 5m }
        };

        // Act
        var result = PortfolioCalculator.Calculate(transactions);

        // Assert
        var sell1Dto = transactions.Single(t => t.Id == sell1Id);
        var sell2Dto = transactions.Single(t => t.Id == sell2Id);

        var sell1DictValue = result.RealizedPnLByTransactionId[sell1Id];
        var sell2DictValue = result.RealizedPnLByTransactionId[sell2Id];

        sell1Dto.RealizedPnL.Should().Be(sell1DictValue.RealizedPnL);
        sell1Dto.RealizedPnLPct.Should().Be(sell1DictValue.RealizedPnLPct);
        sell2Dto.RealizedPnL.Should().Be(sell2DictValue.RealizedPnL);
        sell2Dto.RealizedPnLPct.Should().Be(sell2DictValue.RealizedPnLPct);

        sell1Dto.RealizedPnL.Should().NotBeNull();
        sell2Dto.RealizedPnL.Should().NotBeNull();
    }

    [Fact]
    public void Calculate_MoneyConservation_RemainingCostBasisPlusConsumedCostBasis_ShouldEqualTotalBuyCost()
    {
        // Arrange — the fundamental invariant for any FIFO ledger: nothing may be created or
        // destroyed. Sum of all Buy cost basis must always equal (remaining lot cost basis) +
        // (cost basis consumed by all sells), regardless of splits.
        //
        // Buy A: 10 @ 100, fee 5   -> cost 1005
        // Buy B: 10 @ 200, fee 10  -> cost 2010
        // Total buy cost = 3015
        //
        // 2-for-1 split -> 40 shares total, cost basis unchanged (3015)
        //   Lot A: 20 shares, cost 1005 (50.25/share)
        //   Lot B: 20 shares, cost 2010 (100.5/share)
        //
        // Sell 25 @ 150, fee 20:
        //   Consume all 20 from Lot A: cost 1005
        //   Consume 5 from Lot B: cost 5 * 100.5 = 502.5
        //   costBasisConsumed = 1507.5
        //   netProceeds = 25*150 - 20 = 3730
        //   RealizedPnL = 3730 - 1507.5 = 2222.5
        //   Remaining Lot B: 15 shares, cost 2010 - 502.5 = 1507.5
        var sellId = Guid.NewGuid();
        var transactions = new List<PortfolioTransactionDto>
        {
            new() { Id = Guid.NewGuid(), TransactionType = TransactionType.Buy, Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 10m, SharePrice = 100m, Fees = 5m },
            new() { Id = Guid.NewGuid(), TransactionType = TransactionType.Buy, Date = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 10m, SharePrice = 200m, Fees = 10m },
            new() { Id = Guid.NewGuid(), TransactionType = TransactionType.Split, Date = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 2m, SharePrice = 0m, Fees = 0m },
            new() { Id = sellId, TransactionType = TransactionType.Sell, Date = new DateTime(2024, 1, 4, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 25m, SharePrice = 150m, Fees = 20m }
        };

        const decimal totalBuyCost = 3015m;

        // Act
        var result = PortfolioCalculator.Calculate(transactions);

        // Assert
        result.TotalShares.Should().Be(15m);
        result.CostBasis.Should().Be(1507.5m);

        var sellResult = result.RealizedPnLByTransactionId[sellId];
        sellResult.RealizedPnL.Should().Be(2222.5m);

        // Net proceeds (3730) - RealizedPnL (2222.5) = cost basis consumed (1507.5)
        var netProceeds = 25m * 150m - 20m;
        var costBasisConsumed = netProceeds - sellResult.RealizedPnL!.Value;

        (result.CostBasis + costBasisConsumed).Should().Be(totalBuyCost);
    }

    [Fact]
    public void Calculate_SellExactlyDepletingOneLot_ShouldRemoveLotCleanlyWithNoRemainder()
    {
        // Arrange — boundary: sell quantity exactly equal to one full lot, next lot untouched
        var transactions = new List<PortfolioTransactionDto>
        {
            new() { Id = Guid.NewGuid(), TransactionType = TransactionType.Buy, Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 10m, SharePrice = 100m, Fees = 0m },
            new() { Id = Guid.NewGuid(), TransactionType = TransactionType.Buy, Date = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 10m, SharePrice = 200m, Fees = 0m },
            new() { Id = Guid.NewGuid(), TransactionType = TransactionType.Sell, Date = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 10m, SharePrice = 150m, Fees = 0m }
        };

        // Act
        var result = PortfolioCalculator.Calculate(transactions);

        // Assert — only the second lot (10 @ 200) should remain untouched
        result.TotalShares.Should().Be(10m);
        result.CostBasis.Should().Be(2000m);
    }

    [Fact]
    public void Calculate_WithRepeatingDecimalCostPerShare_ShouldRoundConsistentlyAndConserveMoney()
    {
        // Arrange — 1 buy of 3 shares forces a non-terminating cost-per-share (1000/3 = 333.33...).
        // Selling 1 share must not silently lose or fabricate cents beyond the 8dp rounding rule
        // already applied to quantities; verify the conservation invariant still holds to the cent.
        var sellId = Guid.NewGuid();
        var transactions = new List<PortfolioTransactionDto>
        {
            new() { Id = Guid.NewGuid(), TransactionType = TransactionType.Buy, Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 3m, SharePrice = 333.33m, Fees = 1m },
            new() { Id = sellId, TransactionType = TransactionType.Sell, Date = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc), SharesQuantity = 1m, SharePrice = 400m, Fees = 0m }
        };

        const decimal totalBuyCost = (3m * 333.33m) + 1m; // 1000.99

        // Act
        var result = PortfolioCalculator.Calculate(transactions);

        // Assert
        result.TotalShares.Should().Be(2m);

        var sellResult = result.RealizedPnLByTransactionId[sellId];
        sellResult.RealizedPnL.Should().NotBeNull();

        var netProceeds = 1m * 400m;
        var costBasisConsumed = netProceeds - sellResult.RealizedPnL!.Value;

        (result.CostBasis + costBasisConsumed).Should().Be(totalBuyCost);
    }
}
