using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Calculates portfolio position metrics using FIFO (First-In, First-Out) cost basis method.
/// This is a pure calculation service with no dependencies.
/// </summary>
public static class PortfolioCalculator
{
    private class BuyLot
    {
        public decimal Quantity { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// Result of a single FIFO pass over a security's transactions: position metrics
    /// (shares, average price, cost basis) and realized PnL per sell transaction.
    /// </summary>
    public sealed record FifoCalculationResult(
        decimal TotalShares,
        decimal AverageSharePrice,
        decimal CostBasis,
        IReadOnlyDictionary<Guid, (decimal? RealizedPnL, decimal? RealizedPnLPct)> RealizedPnLByTransactionId);

    /// <summary>
    /// Calculates position metrics and realized PnL using the FIFO (First-In, First-Out) cost basis method.
    /// Processes transactions chronologically (ordered by date ascending) in a single pass:
    /// - Buys add new lots
    /// - Sells consume lots from oldest to newest, and their realized PnL is both written onto
    ///   the transaction DTO and returned in <see cref="FifoCalculationResult.RealizedPnLByTransactionId"/>
    /// - Splits multiply existing shares in all lots while keeping their cost basis unchanged
    /// - Dividends don't affect cost basis
    /// </summary>
    public static FifoCalculationResult Calculate(List<PortfolioTransactionDto> transactions)
    {
        if (transactions.Count == 0)
        {
            return new FifoCalculationResult(0, 0, 0, new Dictionary<Guid, (decimal?, decimal?)>());
        }

        // CRITICAL: Order by date ascending to process transactions chronologically.
        // For transactions on the same day, process splits first (they take effect at market open),
        // then buys (post-split), then sells (at post-split quantities).
        // CreatedAt is used as final tiebreaker within the same type and date.
        var orderedTransactions = transactions
            .OrderBy(t => t.Date)
            .ThenBy(t => TransactionOrdering.GetSortOrder(t.TransactionType))
            .ThenBy(t => t.CreatedAt)
            .ToList();

        var lots = new List<BuyLot>();
        var realizedPnLByTransactionId = new Dictionary<Guid, (decimal? RealizedPnL, decimal? RealizedPnLPct)>();

        foreach (var transaction in orderedTransactions)
        {
            switch (transaction.TransactionType)
            {
                case TransactionType.Buy:
                    lots.Add(new BuyLot
                    {
                        Quantity = transaction.SharesQuantity,
                        TotalCost = (transaction.SharesQuantity * transaction.SharePrice) + transaction.Fees
                    });
                    realizedPnLByTransactionId[transaction.Id] = (null, null);
                    break;
                case TransactionType.Sell:
                    realizedPnLByTransactionId[transaction.Id] = ProcessSellTransactionFIFO(transaction, lots);
                    break;
                case TransactionType.Split:
                    if (transaction.SharesQuantity > 0)
                    {
                        foreach (var lot in lots)
                        {
                            lot.Quantity *= transaction.SharesQuantity;
                        }
                    }
                    realizedPnLByTransactionId[transaction.Id] = (null, null);
                    break;
                default:
                    realizedPnLByTransactionId[transaction.Id] = (null, null);
                    break;
            }

            foreach (var lot in lots)
            {
                lot.Quantity = Math.Round(lot.Quantity, 8);
            }
            lots.RemoveAll(l => l.Quantity <= 0);
        }

        var totalShares = Math.Round(lots.Sum(l => l.Quantity), 8);
        var costBasis = lots.Sum(l => l.TotalCost);
        var averageSharePrice = totalShares > 0 ? costBasis / totalShares : 0;

        return new FifoCalculationResult(totalShares, averageSharePrice, costBasis, realizedPnLByTransactionId);
    }

    /// <summary>
    /// Processes a sell transaction using FIFO method.
    /// Consumes shares from oldest lots first, writes realized PnL onto the transaction DTO,
    /// and returns the same values for the caller's per-transaction result map.
    /// </summary>
    private static (decimal? RealizedPnL, decimal? RealizedPnLPct) ProcessSellTransactionFIFO(
        PortfolioTransactionDto transaction, List<BuyLot> lots)
    {
        var totalAvailable = lots.Sum(l => l.Quantity);
        if (totalAvailable <= 0)
        {
            transaction.RealizedPnL = null;
            transaction.RealizedPnLPct = null;
            return (null, null);
        }

        var sharesToSell = Math.Min(transaction.SharesQuantity, totalAvailable);
        if (sharesToSell <= 0)
        {
            transaction.RealizedPnL = null;
            transaction.RealizedPnLPct = null;
            return (null, null);
        }

        decimal costBasisConsumed = 0;
        decimal remainingToSell = sharesToSell;

        while (remainingToSell > 0 && lots.Count > 0)
        {
            var lot = lots[0];
            var take = Math.Min(remainingToSell, lot.Quantity);

            // Proportional cost basis for the quantity taken from this lot
            var lotCostBasisToRemove = take * (lot.TotalCost / lot.Quantity);

            costBasisConsumed += lotCostBasisToRemove;

            lot.Quantity -= take;
            lot.TotalCost -= lotCostBasisToRemove;
            remainingToSell -= take;

            if (lot.Quantity <= 0)
            {
                lots.RemoveAt(0);
            }
        }

        // Realized P/L = Net Proceeds - Cost Basis
        // Net Proceeds = (Shares * Price) - Fees - Tax
        var netProceeds = (sharesToSell * transaction.SharePrice) - transaction.Fees;
        transaction.RealizedPnL = netProceeds - costBasisConsumed;
        transaction.RealizedPnLPct = costBasisConsumed > 0
            ? (transaction.RealizedPnL / costBasisConsumed) * 100
            : null;

        return (transaction.RealizedPnL, transaction.RealizedPnLPct);
    }

    /// <summary>
    /// Calculates the current allocation percentage for a position.
    /// </summary>
    /// <param name="positionMarketValue">Current market value of the position</param>
    /// <param name="totalPortfolioValue">Total market value of the portfolio</param>
    /// <returns>Current allocation percentage (0-100)</returns>
    public static decimal CalculateCurrentAllocationPercentage(decimal positionMarketValue, decimal totalPortfolioValue)
    {
        if (totalPortfolioValue == 0)
        {
            return 0;
        }

        return (positionMarketValue / totalPortfolioValue) * 100;
    }

    /// <summary>
    /// Calculates the rebalancing amount needed to reach target allocation.
    /// </summary>
    /// <param name="currentMarketValue">Current market value of the position</param>
    /// <param name="targetPercentage">Target allocation percentage</param>
    /// <param name="totalPortfolioValue">Total market value of the portfolio</param>
    /// <returns>Rebalancing amount (positive = buy, negative = sell)</returns>
    public static decimal CalculateRebalancingAmount(decimal currentMarketValue, decimal targetPercentage, decimal totalPortfolioValue)
    {
        var targetMarketValue = (targetPercentage / 100) * totalPortfolioValue;
        return targetMarketValue - currentMarketValue;
    }

    /// <summary>
    /// Determines the rebalancing status based on current and target allocation.
    /// </summary>
    /// <param name="currentAllocation">Current allocation percentage</param>
    /// <param name="targetAllocation">Target allocation percentage</param>
    /// <returns>Rebalancing status</returns>
    public static RebalancingStatus DetermineRebalancingStatus(decimal currentAllocation, decimal targetAllocation)
    {
        const decimal rebalancingThreshold = 0.5m; // Threshold in percentage points
        var deviation = Math.Abs(currentAllocation - targetAllocation);

        // Balanced if within ±1% of target
        // Using <= with explicit decimal comparison to avoid precision issues
        if (deviation <= rebalancingThreshold)
        {
            return RebalancingStatus.Balanced;
        }

        return currentAllocation > targetAllocation ? RebalancingStatus.Overweight : RebalancingStatus.Underweight;
    }
}

