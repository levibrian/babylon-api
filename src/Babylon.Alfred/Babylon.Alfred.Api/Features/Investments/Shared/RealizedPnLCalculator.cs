using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Calculates realized PnL for sell transactions using FIFO cost basis.
/// </summary>
public static class RealizedPnLCalculator
{
    private class BuyLot
    {
        public decimal Quantity { get; set; }
        public decimal TotalCost { get; set; }
    }
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

            currentShares = Math.Round(currentShares, 8);
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
        var lots = new List<BuyLot>();

        foreach (var transaction in orderedTransactions)
        {
            switch (transaction.TransactionType)
            {
                case TransactionType.Buy:
                    results[transaction.Id] = (null, null);
                    lots.Add(new BuyLot
                    {
                        Quantity = transaction.SharesQuantity,
                        TotalCost = (transaction.SharesQuantity * transaction.SharePrice) + transaction.Fees + transaction.Tax
                    });
                    break;
                case TransactionType.Sell:
                    results[transaction.Id] = CalculateFIFOSellPnL(transaction, lots);
                    ConsumeLots(lots, transaction.SharesQuantity);
                    break;
                case TransactionType.Split:
                    results[transaction.Id] = (null, null);
                    if (transaction.SharesQuantity > 0)
                    {
                        foreach (var lot in lots)
                        {
                            lot.Quantity *= transaction.SharesQuantity;
                        }
                    }
                    break;
                default:
                    results[transaction.Id] = (null, null);
                    break;
            }

            // Round quantities in lots to prevent precision drift
            foreach (var lot in lots)
            {
                lot.Quantity = Math.Round(lot.Quantity, 8);
            }

            // Remove lots with zero or negative quantity (negative shouldn't happen with inventory validation)
            lots.RemoveAll(l => l.Quantity <= 0);
        }

        return results;
    }

    private static (decimal? RealizedPnL, decimal? RealizedPnLPct) CalculateFIFOSellPnL(
        PortfolioTransactionDto transaction,
        List<BuyLot> lots)
    {
        var totalAvailable = lots.Sum(l => l.Quantity);
        if (totalAvailable <= 0)
        {
            return (null, null);
        }

        var sharesToSell = Math.Min(transaction.SharesQuantity, totalAvailable);
        if (sharesToSell <= 0)
        {
            return (null, null);
        }

        decimal costBasisConsumed = 0;
        decimal remainingToSell = sharesToSell;

        foreach (var lot in lots)
        {
            if (remainingToSell <= 0) break;

            var take = Math.Min(remainingToSell, lot.Quantity);
            // Proportional cost basis for the quantity taken from this lot
            costBasisConsumed += take * (lot.TotalCost / lot.Quantity);
            remainingToSell -= take;
        }

        var grossProceeds = transaction.SharePrice * sharesToSell;
        var netProceeds = grossProceeds - transaction.Fees - transaction.Tax;
        var realizedPnL = netProceeds - costBasisConsumed;

        if (costBasisConsumed <= 0)
        {
            return (realizedPnL, null);
        }

        var realizedPnLPct = (realizedPnL / costBasisConsumed) * 100;
        return (realizedPnL, realizedPnLPct);
    }

    private static void ConsumeLots(List<BuyLot> lots, decimal quantityToSell)
    {
        decimal remainingToSell = quantityToSell;
        while (remainingToSell > 0 && lots.Count > 0)
        {
            var lot = lots[0];
            var take = Math.Min(remainingToSell, lot.Quantity);

            // Proportional cost reduction
            var costToRemove = take * (lot.TotalCost / lot.Quantity);
            lot.Quantity -= take;
            lot.TotalCost -= costToRemove;
            remainingToSell -= take;

            if (lot.Quantity <= 0)
            {
                lots.RemoveAt(0);
            }
        }
    }
}
