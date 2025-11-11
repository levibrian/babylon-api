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
    /// Calculates the cost basis and total shares using weighted average cost method.
    /// Processes transactions chronologically: buys add shares and cost, sells reduce proportionally.
    /// </summary>
    public static (decimal totalShares, decimal costBasis) CalculateCostBasis(List<PortfolioTransactionDto> transactions)
    {
        decimal totalShares = 0;
        decimal costBasis = 0;

        var orderedTransactions = transactions.OrderBy(t => t.Date).ToList();

        foreach (var transaction in orderedTransactions)
        {
            (totalShares, costBasis) = transaction.TransactionType switch
            {
                TransactionType.Buy => ProcessBuyTransaction(transaction, totalShares, costBasis),
                TransactionType.Sell => ProcessSellTransaction(transaction, totalShares, costBasis),
                _ => (totalShares, costBasis)
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
        var deviation = Math.Abs(currentAllocation - targetAllocation);
        
        // Balanced if within ±1% of target
        if (deviation <= 1)
        {
            return RebalancingStatus.Balanced;
        }

        return currentAllocation > targetAllocation ? RebalancingStatus.Overweight : RebalancingStatus.Underweight;
    }
}

