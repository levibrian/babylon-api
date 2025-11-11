using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class PortfolioService(
    ITransactionRepository transactionRepository,
    ICompanyRepository companyRepository,
    IMarketPriceService marketPriceService,
    IAllocationStrategyService allocationStrategyService) : IPortfolioService
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
        var positions = await CreatePositionsAsync(groupedTransactions, effectiveUserId);

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
    /// Fetches all companies, market prices, and allocation strategies, then processes everything in memory.
    /// </summary>
    private async Task<List<PortfolioPositionDto>> CreatePositionsAsync(
        List<IGrouping<string, Transaction>> groupedTransactions,
        Guid userId)
    {
        // Fetch all companies in a single database query
        var tickers = groupedTransactions.Select(g => g.Key).ToList();
        var companiesLookup = await companyRepository.GetByTickersAsync(tickers);

        // Fetch market prices for all positions
        var marketPrices = await marketPriceService.GetCurrentPricesAsync(tickers);

        // Fetch target allocations
        var targetAllocations = await allocationStrategyService.GetTargetAllocationsAsync(userId);

        // Calculate total portfolio market value
        var totalPortfolioValue = groupedTransactions.Sum(group =>
        {
            var ticker = group.Key;
            var positionTransactions = MapToTransactionDtos(group);
            var (totalShares, _) = PortfolioCalculator.CalculatePositionMetrics(positionTransactions);
            var currentPrice = marketPrices.GetValueOrDefault(ticker, 0);
            return totalShares * currentPrice;
        });

        // Process all positions in memory
        return groupedTransactions
            .Select(group => CreatePosition(
                group,
                companiesLookup.GetValueOrDefault(group.Key),
                marketPrices,
                targetAllocations,
                totalPortfolioValue))
            .ToList();
    }

    /// <summary>
    /// Creates a single position DTO from a group of transactions, company information, market prices, and allocation data.
    /// </summary>
    private static PortfolioPositionDto CreatePosition(
        IGrouping<string, Transaction> transactionGroup,
        Company? company,
        Dictionary<string, decimal> marketPrices,
        Dictionary<string, decimal> targetAllocations,
        decimal totalPortfolioValue)
    {
        var ticker = transactionGroup.Key;
        var positionTransactions = MapToTransactionDtos(transactionGroup);
        var (totalShares, averageSharePrice) = PortfolioCalculator.CalculatePositionMetrics(positionTransactions);
        var totalInvested = transactionGroup.Sum(t => t.TotalAmount);

        // Calculate market value and allocation
        var currentPrice = marketPrices.GetValueOrDefault(ticker, 0);
        var currentMarketValue = totalShares * currentPrice;
        var currentAllocation = PortfolioCalculator.CalculateCurrentAllocationPercentage(currentMarketValue, totalPortfolioValue);

        // Get target allocation if set
        var targetAllocation = targetAllocations.GetValueOrDefault(ticker);
        var hasTargetAllocation = targetAllocations.ContainsKey(ticker);

        // Calculate deviation and rebalancing amount if target allocation exists
        decimal? allocationDeviation = null;
        decimal? rebalancingAmount = null;
        var rebalancingStatus = RebalancingStatus.Balanced;
        string? rebalancingMessage = null;

        if (hasTargetAllocation)
        {
            allocationDeviation = currentAllocation - targetAllocation;
            rebalancingAmount = PortfolioCalculator.CalculateRebalancingAmount(currentMarketValue, targetAllocation, totalPortfolioValue);
            rebalancingStatus = PortfolioCalculator.DetermineRebalancingStatus(currentAllocation, targetAllocation);

            // Generate message if not balanced
            if (rebalancingStatus != RebalancingStatus.Balanced)
            {
                var status = rebalancingStatus == RebalancingStatus.Overweight ? "Overweight" : "Underweight";
                var action = rebalancingStatus == RebalancingStatus.Overweight ? "Sell" : "Buy";
                rebalancingMessage = $"{Math.Abs(allocationDeviation.Value):F1}% {status} {action} ~€{Math.Abs(rebalancingAmount.Value):F0}";
            }
        }

        return new PortfolioPositionDto
        {
            Ticker = ticker,
            CompanyName = company?.CompanyName ?? ticker,
            TotalInvested = totalInvested,
            TotalShares = totalShares,
            AverageSharePrice = averageSharePrice,
            CurrentMarketValue = currentMarketValue > 0 ? currentMarketValue : null,
            CurrentAllocationPercentage = totalPortfolioValue > 0 ? currentAllocation : null,
            TargetAllocationPercentage = hasTargetAllocation ? targetAllocation : null,
            AllocationDeviation = allocationDeviation,
            RebalancingAmount = rebalancingAmount,
            RebalancingStatus = rebalancingStatus,
            RebalancingMessage = rebalancingMessage,
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
