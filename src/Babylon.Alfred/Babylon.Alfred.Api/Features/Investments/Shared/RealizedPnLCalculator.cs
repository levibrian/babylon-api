using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Calculates realized PnL for sell transactions using weighted average cost basis.
/// </summary>
public static class RealizedPnLCalculator
{
    public static IReadOnlyDictionary<Guid, decimal> CalculateAvailableSharesBeforeTransaction(
        IEnumerable<PortfolioTransactionDto> transactions)
    {
        var orderedTransactions = transactions
            .OrderBy(t => t.Date)
            .ThenBy(t => t.CreatedAt)
            .ToList();

        var sharesBefore = new Dictionary<Guid, decimal>();
        decimal currentShares = 0;

        foreach (var transaction in orderedTransactions)
        {
            sharesBefore[transaction.Id] = currentShares;

            switch (transaction.TransactionType)
            {
                case TransactionType.Buy:
                    currentShares += transaction.SharesQuantity;
                    break;
                case TransactionType.Sell:
                    var sharesToSell = Math.Min(transaction.SharesQuantity, currentShares);
                    currentShares -= sharesToSell;
                    break;
                case TransactionType.Split:
                    if (currentShares > 0 && transaction.SharesQuantity > 0)
                    {
                        currentShares *= transaction.SharesQuantity;
                    }
                    break;
            }
        }

        return sharesBefore;
    }

    public static IReadOnlyDictionary<Guid, (decimal? RealizedPnL, decimal? RealizedPnLPct)>
        CalculateRealizedPnLByTransactionId(IEnumerable<PortfolioTransactionDto> transactions)
    {
        var orderedTransactions = transactions
            .OrderBy(t => t.Date)
            .ThenBy(t => t.CreatedAt)
            .ToList();

        var results = new Dictionary<Guid, (decimal? RealizedPnL, decimal? RealizedPnLPct)>();
        decimal currentShares = 0;
        decimal currentCostBasis = 0;

        foreach (var transaction in orderedTransactions)
        {
            switch (transaction.TransactionType)
            {
                case TransactionType.Buy:
                    results[transaction.Id] = (null, null);
                    currentShares += transaction.SharesQuantity;
                    currentCostBasis += (transaction.SharesQuantity * transaction.SharePrice) + transaction.Fees;
                    break;
                case TransactionType.Sell:
                    results[transaction.Id] = CalculateSellRealizedPnL(transaction, currentShares, currentCostBasis);

                    if (currentShares > 0)
                    {
                        var averageCostPerShare = currentCostBasis / currentShares;
                        var sharesToSell = Math.Min(transaction.SharesQuantity, currentShares);
                        var costBasisToRemove = sharesToSell * averageCostPerShare;

                        currentShares -= sharesToSell;
                        currentCostBasis -= costBasisToRemove;

                        if (currentShares == 0)
                        {
                            currentCostBasis = 0;
                        }
                    }
                    break;
                case TransactionType.Split:
                    results[transaction.Id] = (null, null);
                    if (currentShares > 0 && transaction.SharesQuantity > 0)
                    {
                        currentShares *= transaction.SharesQuantity;
                    }
                    break;
                default:
                    results[transaction.Id] = (null, null);
                    break;
            }
        }

        return results;
    }

    private static (decimal? RealizedPnL, decimal? RealizedPnLPct) CalculateSellRealizedPnL(
        PortfolioTransactionDto transaction,
        decimal currentShares,
        decimal currentCostBasis)
    {
        if (currentShares <= 0)
        {
            return (null, null);
        }

        var sharesToSell = Math.Min(transaction.SharesQuantity, currentShares);
        if (sharesToSell <= 0)
        {
            return (null, null);
        }

        var averageCostPerShare = currentCostBasis / currentShares;
        if (averageCostPerShare <= 0)
        {
            return (null, null);
        }

        var grossProceeds = transaction.SharePrice * sharesToSell;
        var netProceeds = grossProceeds - transaction.Fees;
        var soldCostBasis = sharesToSell * averageCostPerShare;
        var realizedPnL = netProceeds - soldCostBasis;

        if (soldCostBasis <= 0)
        {
            return (realizedPnL, null);
        }

        var realizedPnLPct = (realizedPnL / soldCostBasis) * 100;
        return (realizedPnL, realizedPnLPct);
    }

}
