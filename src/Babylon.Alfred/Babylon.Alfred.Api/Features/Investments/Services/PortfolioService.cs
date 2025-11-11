using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class PortfolioService(ITransactionRepository transactionRepository, ICompanyRepository companyRepository) : IPortfolioService
{
    public async Task<PortfolioResponse> GetPortfolio(Guid? userId)
    {
        var effectiveUserId = userId ?? Constants.User.RootUserId;
        var transactions = (await transactionRepository.GetOpenPositionsByUser(effectiveUserId)).ToList();

        if (transactions.Count == 0)
        {
            return new PortfolioResponse
            {
                Positions = [],
                TotalInvested = 0
            };
        }

        var groupedTransactions = transactions.GroupBy(t => t.Ticker).ToList();
        var positions = await CreatePositionsAsync(groupedTransactions);

        var orderedPositions = positions
            .OrderByDescending(p => p.TotalInvested)
            .ToList();

        return new PortfolioResponse
        {
            Positions = orderedPositions,
            TotalInvested = orderedPositions.Sum(p => p.TotalInvested)
        };
    }

    /// <summary>
    /// Creates position DTOs from grouped transactions.
    /// Fetches all companies in a single database query, then processes everything in memory.
    /// </summary>
    private async Task<List<PortfolioPositionDto>> CreatePositionsAsync(
        List<IGrouping<string, Transaction>> groupedTransactions)
    {
        // Fetch all companies in a single database query
        var tickers = groupedTransactions.Select(g => g.Key).ToList();
        var companiesLookup = await companyRepository.GetByTickersAsync(tickers);

        // Process all positions in memory (can use parallel processing here if needed)
        return groupedTransactions
            .Select(group => CreatePosition(group, companiesLookup.GetValueOrDefault(group.Key)))
            .ToList();
    }

    /// <summary>
    /// Creates a single position DTO from a group of transactions and company information.
    /// </summary>
    private static PortfolioPositionDto CreatePosition(
        IGrouping<string, Transaction> transactionGroup,
        Company? company)
    {
        var ticker = transactionGroup.Key;
        var positionTransactions = MapToTransactionDtos(transactionGroup);
        var (totalShares, averageSharePrice) = PortfolioCalculator.CalculatePositionMetrics(positionTransactions);
        var totalInvested = transactionGroup.Sum(t => t.TotalAmount);

        return new PortfolioPositionDto
        {
            Ticker = ticker,
            CompanyName = company?.CompanyName ?? ticker,
            TotalInvested = totalInvested,
            TotalShares = totalShares,
            AverageSharePrice = averageSharePrice,
            Transactions = positionTransactions
        };
    }

    /// <summary>
    /// Maps domain transactions to DTOs, ordered by date descending (newest first).
    /// </summary>
    private static List<PortfolioTransactionDto> MapToTransactionDtos(
        IEnumerable<Transaction> transactions)
    {
        return transactions
            .Select(t => new PortfolioTransactionDto
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                Date = t.Date,
                SharesQuantity = t.SharesQuantity,
                SharePrice = t.SharePrice,
                Fees = t.Fees,
                TotalAmount = t.TotalAmount
            })
            .OrderByDescending(t => t.Date)
            .ToList();
    }

}
