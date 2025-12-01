using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Calculates portfolio position metrics using weighted average cost basis method.
/// This is a pure calculation service with no dependencies.
/// </summary>
public static class PortfolioCalculator
{
    /// <summary>
    /// Calculates position metrics (total shares and average share price) using weighted average cost basis.
    /// Processes transactions chronologically to handle buys and sells correctly.
    /// </summary>
    /// <param name="transactions">List of transactions ordered by date (descending for display, will be reordered internally)</param>
    /// <returns>Tuple containing total shares and average share price</returns>
    public static (decimal totalShares, decimal averageSharePrice) CalculatePositionMetrics(
        List<PortfolioTransactionDto> transactions)
    {
        if (transactions.Count == 0)
        {
            return (0, 0);
        }

        var (totalShares, costBasis) = CalculateCostBasis(transactions);
        var averageSharePrice = totalShares > 0 ? costBasis / totalShares : 0;

        return (totalShares, averageSharePrice);
    }

    /// <summary>
    /// Calculates the cost basis and total shares using the weighted average cost method.
    /// Processes transactions chronologically (ordered by date ascending):
    /// - Buys add shares and cost
    /// - Sells reduce shares and cost proportionally
    /// - Splits multiply existing shares (only those held before split date) while keeping cost basis unchanged
    /// - Dividends don't affect cost basis
    /// </summary>
    public static (decimal totalShares, decimal costBasis) CalculateCostBasis(List<PortfolioTransactionDto> transactions)
    {
        decimal totalShares = 0;
        decimal costBasis = 0;

        // CRITICAL: Order by date ascending to process transactions chronologically
        // This ensures splits only affect shares that existed before the split date
        var orderedTransactions = transactions.OrderBy(t => t.Date).ToList();

        foreach (var transaction in orderedTransactions)
        {
            (totalShares, costBasis) = transaction.TransactionType switch
            {
                TransactionType.Buy => ProcessBuyTransaction(transaction, totalShares, costBasis),
                TransactionType.Sell => ProcessSellTransaction(transaction, totalShares, costBasis),
                TransactionType.Split => ProcessSplitTransaction(transaction, totalShares, costBasis),
                _ => (totalShares, costBasis) // Dividends don't affect cost basis
            };
        }

        return (totalShares, costBasis);
    }

    /// <summary>
    /// Processes a buy transaction by adding shares and cost (including fees) to the position.
    /// </summary>
    private static (decimal totalShares, decimal costBasis) ProcessBuyTransaction(
        PortfolioTransactionDto transaction,
        decimal currentShares,
        decimal currentCostBasis)
    {
        var newShares = currentShares + transaction.SharesQuantity;
        var transactionCost = (transaction.SharesQuantity * transaction.SharePrice) + transaction.Fees;
        var newCostBasis = currentCostBasis + transactionCost;

        return (newShares, newCostBasis);
    }

    /// <summary>
    /// Processes a sell transaction using weighted average cost method.
    /// Reduces shares and cost basis proportionally based on the average cost per share.
    /// </summary>
    private static (decimal totalShares, decimal costBasis) ProcessSellTransaction(
        PortfolioTransactionDto transaction,
        decimal currentShares,
        decimal currentCostBasis)
    {
        if (currentShares == 0)
        {
            // Cannot sell shares we don't have - ignore this transaction
            return (currentShares, currentCostBasis);
        }

        var averageCostPerShare = currentCostBasis / currentShares;
        var sharesToSell = Math.Min(transaction.SharesQuantity, currentShares);
        var costBasisToRemove = sharesToSell * averageCostPerShare;

        var remainingShares = currentShares - sharesToSell;
        var remainingCostBasis = currentCostBasis - costBasisToRemove;

        // Ensure cost basis is zero when no shares remain (handles floating point precision)
        if (remainingShares == 0)
        {
            remainingCostBasis = 0;
        }

        return (remainingShares, remainingCostBasis);
    }

    /// <summary>
    /// Processes a stock split transaction.
    /// Multiplies existing shares held at the split date by the split ratio while keeping cost basis unchanged.
    ///
    /// IMPORTANT: Only affects shares that exist BEFORE the split date (chronological processing).
    /// Any shares purchased AFTER the split date are added normally and are not affected by the split.
    ///
    /// Example: 2-for-1 split on March 1st
    /// - Jan 1: Buy 100 shares → currentShares = 100
    /// - Feb 1: Buy 50 shares → currentShares = 150
    /// - Mar 1: Split 2-for-1 → currentShares = 300 (150 × 2.0), cost basis unchanged
    /// - Apr 1: Buy 20 shares → currentShares = 320 (split doesn't affect new purchase)
    ///
    /// For example, a 2-for-1 split (SharesQuantity = 2.0) doubles the shares held at that date.
    /// </summary>
    /// <param name="transaction">The split transaction (SharesQuantity = split ratio, SharePrice = 0)</param>
    /// <param name="currentShares">Current number of shares held BEFORE the split date (accumulated from all prior transactions)</param>
    /// <param name="currentCostBasis">Current cost basis (unchanged by splits)</param>
    /// <returns>Updated shares and cost basis</returns>
    private static (decimal totalShares, decimal costBasis) ProcessSplitTransaction(
        PortfolioTransactionDto transaction,
        decimal currentShares,
        decimal currentCostBasis)
    {
        if (currentShares == 0)
        {
            // No shares held at split date - ignore this transaction
            return (currentShares, currentCostBasis);
        }

        if (transaction.SharesQuantity <= 0)
        {
            // Invalid split ratio - ignore this transaction
            return (currentShares, currentCostBasis);
        }

        // Multiply shares held at split date by split ratio, cost basis remains the same
        // This automatically adjusts the average cost per share downward
        var newShares = currentShares * transaction.SharesQuantity;

        return (newShares, currentCostBasis);
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

