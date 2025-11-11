using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class PortfolioInsightsService : IPortfolioInsightsService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMarketPriceService _marketPriceService;
    private readonly IAllocationStrategyService _allocationStrategyService;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<PortfolioInsightsService> _logger;

    public PortfolioInsightsService(
        ITransactionRepository transactionRepository,
        IMarketPriceService marketPriceService,
        IAllocationStrategyService allocationStrategyService,
        ICompanyRepository companyRepository,
        ILogger<PortfolioInsightsService> logger)
    {
        _transactionRepository = transactionRepository;
        _marketPriceService = marketPriceService;
        _allocationStrategyService = allocationStrategyService;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<List<PortfolioInsightDto>> GetTopInsightsAsync(Guid userId, int count = 5)
    {
        var insights = new List<PortfolioInsightDto>();

        // Get rebalancing insights
        var rebalancingInsights = await GetRebalancingInsightsAsync(userId);
        insights.AddRange(rebalancingInsights);

        // Future: Add other insight types here (performance milestones, warnings, etc.)
        // var performanceInsights = await GetPerformanceInsightsAsync(userId);
        // insights.AddRange(performanceInsights);

        // Sort by severity (Critical > Warning > Info) and then by absolute deviation
        return insights
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => Math.Abs(i.Amount ?? 0))
            .Take(count)
            .ToList();
    }

    private async Task<List<PortfolioInsightDto>> GetRebalancingInsightsAsync(Guid userId)
    {
        var insights = new List<PortfolioInsightDto>();

        // Get transactions and calculate portfolio
        var transactions = (await _transactionRepository.GetOpenPositionsByUser(userId)).ToList();
        if (transactions.Count == 0)
        {
            return insights;
        }

        // Get target allocations
        var targetAllocations = await _allocationStrategyService.GetTargetAllocationsAsync(userId);
        if (targetAllocations.Count == 0)
        {
            return insights; // No allocation strategy set
        }

        // Get market prices for all positions
        var tickers = transactions.Select(t => t.Ticker).Distinct().ToList();
        var marketPrices = await _marketPriceService.GetCurrentPricesAsync(tickers);

        // Calculate total portfolio market value
        var totalPortfolioValue = transactions
            .GroupBy(t => t.Ticker)
            .Sum(g =>
            {
                var ticker = g.Key;
                var totalShares = g.Sum(t => t.TransactionType == TransactionType.Buy ? t.SharesQuantity : -t.SharesQuantity);
                var price = marketPrices.GetValueOrDefault(ticker, 0);
                return totalShares * price;
            });

        if (totalPortfolioValue == 0)
        {
            return insights; // No portfolio value
        }

        // Calculate current allocations and deviations
        var groupedTransactions = transactions.GroupBy(t => t.Ticker).ToList();
        foreach (var group in groupedTransactions)
        {
            var ticker = group.Key;
            if (!targetAllocations.ContainsKey(ticker))
            {
                continue; // Skip positions without target allocation
            }

            // Map transactions to DTOs for calculation
            var transactionDtos = group.Select(t => new PortfolioTransactionDto
            {
                TransactionType = t.TransactionType,
                Date = t.Date,
                SharesQuantity = t.SharesQuantity,
                SharePrice = t.SharePrice,
                Fees = t.Fees
            }).ToList();

            var (totalShares, _) = PortfolioCalculator.CalculatePositionMetrics(transactionDtos);

            var currentPrice = marketPrices.GetValueOrDefault(ticker, 0);
            var currentMarketValue = totalShares * currentPrice;
            var currentAllocation = totalPortfolioValue > 0 
                ? (currentMarketValue / totalPortfolioValue) * 100 
                : 0;

            var targetAllocation = targetAllocations[ticker];
            var deviation = currentAllocation - targetAllocation;
            var rebalancingAmount = (targetAllocation / 100 * totalPortfolioValue) - currentMarketValue;

            // Only include if deviation is significant (>1%)
            if (Math.Abs(deviation) > 1)
            {
                var status = deviation > 0 ? "Overweight" : "Underweight";
                var action = deviation > 0 ? "Sell" : "Buy";
                var message = $"{Math.Abs(deviation):F1}% {status} {action} ~€{Math.Abs(rebalancingAmount):F0}";

                insights.Add(new PortfolioInsightDto
                {
                    Type = InsightType.Rebalancing,
                    Message = message,
                    Ticker = ticker,
                    Amount = Math.Abs(rebalancingAmount),
                    Severity = InsightSeverity.Warning
                });
            }
        }

        return insights;
    }
}

