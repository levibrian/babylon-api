using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Calculates the running share balance before each transaction, used for oversell validation.
/// FIFO cost basis and realized PnL live in <see cref="PortfolioCalculator"/> — this is a
/// distinct, lighter-weight concern (share count only, no cost tracking).
/// </summary>
public static class RealizedPnLCalculator
{
    public static IReadOnlyDictionary<Guid, decimal> CalculateAvailableSharesBeforeTransaction(
        IEnumerable<PortfolioTransactionDto> transactions)
    {
        var orderedTransactions = transactions
            .OrderBy(t => t.Date)
            .ThenBy(t => TransactionOrdering.GetSortOrder(t.TransactionType))
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

            currentShares = Math.Round(currentShares, 8);
        }

        return sharesBefore;
    }
}
