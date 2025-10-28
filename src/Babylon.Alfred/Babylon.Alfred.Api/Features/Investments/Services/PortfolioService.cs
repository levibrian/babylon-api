using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
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
}
