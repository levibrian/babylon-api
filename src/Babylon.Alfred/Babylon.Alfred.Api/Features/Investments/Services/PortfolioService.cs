using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class PortfolioService(ITransactionRepository transactionRepository, ICompanyRepository companyRepository) : IPortfolioService
{
    public async Task<PortfolioResponse> GetPortfolio(Guid? userId)
    {
        // Get all Buy transactions for the user
        var transactions = await transactionRepository.GetOpenPositionsByUser(userId ?? Constants.User.RootUserId);

        // Group by ticker
        var groupedByTicker = transactions.GroupBy(t => t.Ticker);

        var positions = new List<PortfolioPositionDto>();

        foreach (var group in groupedByTicker)
        {
            var ticker = group.Key;

            // Get company info
            var company = await companyRepository.GetByTickerAsync(ticker);

            var position = new PortfolioPositionDto
            {
                Ticker = ticker,
                CompanyName = company?.CompanyName ?? ticker, // Fallback to ticker if company not found
                TotalInvested = group.Sum(t => t.TotalAmount),
                Transactions = group.Select(t => new PortfolioTransactionDto
                    {
                        Id = t.Id,
                        TransactionType = t.TransactionType,
                        Date = t.Date,
                        SharesQuantity = t.SharesQuantity,
                        SharePrice = t.SharePrice,
                        Fees = t.Fees,
                        Amount = t.Amount,
                        TotalAmount = t.TotalAmount
                    })
                    .OrderByDescending(t => t.Date)
                    .ToList()
            };

            CalculatePositionMetrics(position);

            positions.Add(position);
        }

        // Order positions by total invested (descending)
        positions = positions
            .OrderByDescending(p => p.TotalInvested)
            .ToList();

        return new PortfolioResponse
        {
            Positions = positions,
            TotalInvested = positions.Sum(p => p.TotalInvested)
        };
    }

    private static void CalculatePositionMetrics(PortfolioPositionDto position)
    {
        decimal totalShares = 0;
        decimal totalCostOfBuys = 0;

        foreach (var transaction in
                 position.Transactions.OrderBy(t =>
                     t.Date)) // Assuming a Date property for correct chronological processing
        {
            if (transaction.TransactionType == TransactionType.Buy)
            {
                // For Buys: Add to shares and total cost
                totalShares += transaction.SharesQuantity;
                totalCostOfBuys += transaction.SharesQuantity * transaction.SharePrice;
            }
            else if (transaction.TransactionType == TransactionType.Sell)
            {
                // For Sells: Subtract from shares, and adjust total cost by the average price of the *remaining* position

                // **Important Note on Cost Basis:**
                // This calculation uses the "Weighted Average Cost" method for simplicity.
                // When selling, we reduce the totalCostOfBuys by the average cost of the shares being sold.

                // Calculate the cost basis of the shares being sold
                decimal averageCostAtSale = 0;
                if (totalShares > 0)
                {
                    averageCostAtSale = totalCostOfBuys / totalShares;
                }

                // Shares sold cannot exceed total shares
                var sharesSold = Math.Min(transaction.SharesQuantity, totalShares);

                // Reduce total shares and total cost
                totalShares -= sharesSold;
                totalCostOfBuys -= sharesSold * averageCostAtSale;

                // Handle precision issues after selling the entire position
                if (totalShares == 0)
                {
                    totalCostOfBuys = 0;
                }
            }
            // Dividend transactions are ignored for shares/average price calculation.
            // They affect TotalInvested (profit/loss) but not cost basis of shares held.
        }

        // Update the DTO properties
        position.TotalShares = totalShares;

        // Calculate Average Share Price (only if there are shares remaining)
        if (totalShares > 0)
        {
            position.AverageSharePrice = totalCostOfBuys / totalShares;
            position.TotalInvested = totalCostOfBuys;
        }
        else
        {
            position.AverageSharePrice = 0;
            position.TotalInvested = 0;
        }
    }
}
