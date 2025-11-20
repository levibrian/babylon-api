using System.Diagnostics;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Logging;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class PortfolioInsightsService(
    ITransactionRepository transactionRepository,
    IAllocationStrategyService allocationStrategyService,
    ISecurityRepository securityRepository,
    ILogger<PortfolioInsightsService> logger)
    : IPortfolioInsightsService
{
    public async Task<List<PortfolioInsightDto>> GetTopInsightsAsync(Guid userId, int count = 5)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogOperationStart("GetTopInsights", new { UserId = userId, Count = count });
        
        var insights = new List<PortfolioInsightDto>();

        // Get rebalancing insights
        var rebalancingInsights = await GetRebalancingInsightsAsync(userId);
        insights.AddRange(rebalancingInsights);

        // Future: Add other insight types here (performance milestones, warnings, etc.)
        // var performanceInsights = await GetPerformanceInsightsAsync(userId);
        // insights.AddRange(performanceInsights);

        // Sort by severity (Critical > Warning > Info) and then by absolute deviation
        var result = insights
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => Math.Abs(i.Amount ?? 0))
            .Take(count)
            .ToList();

        stopwatch.Stop();
        logger.LogPerformance("GetTopInsights", stopwatch.ElapsedMilliseconds, new { UserId = userId, InsightCount = result.Count });
        logger.LogOperationSuccess("GetTopInsights", new { UserId = userId, Count = result.Count });
        
        return result;
    }

    private async Task<List<PortfolioInsightDto>> GetRebalancingInsightsAsync(Guid userId)
    {
        var insights = new List<PortfolioInsightDto>();

        // Get transactions and calculate portfolio
        var transactions = (await transactionRepository.GetOpenPositionsByUser(userId)).ToList();
        if (transactions.Count == 0)
        {
            return insights;
        }

        // Get target allocations
        var targetAllocations = await allocationStrategyService.GetTargetAllocationsAsync(userId);
        if (targetAllocations.Count == 0)
        {
            return insights; // No allocation strategy set
        }

        // Load securities for transactions
        var securityIds = transactions.Select(t => t.SecurityId).Distinct().ToList();
        var securities = await securityRepository.GetByIdsAsync(securityIds);
        var securitiesLookup = securities.ToDictionary(s => s.Id, s => s);
        var tickerBySecurityId = securities.ToDictionary(s => s.Id, s => s.Ticker);

        // Calculate total portfolio value using total invested (cost basis) instead of market value
        var totalPortfolioValue = transactions.Sum(t => t.TotalAmount);

        if (totalPortfolioValue == 0)
        {
            return insights; // No portfolio value
        }

        // Calculate current allocations and deviations
        var groupedTransactions = transactions.GroupBy(t => t.SecurityId).ToList();
        foreach (var group in groupedTransactions)
        {
            var securityId = group.Key;
            var security = securitiesLookup.GetValueOrDefault(securityId);
            var ticker = security?.Ticker ?? string.Empty;
            if (!targetAllocations.ContainsKey(ticker))
            {
                continue; // Skip positions without target allocation
            }

            // Calculate total invested (cost basis) for this position
            var totalInvested = group.Sum(t => t.TotalAmount);

            // Calculate allocation using total invested (cost basis) instead of market value
            var currentAllocation = totalPortfolioValue > 0 
                ? (totalInvested / totalPortfolioValue) * 100 
                : 0;

            var targetAllocation = targetAllocations[ticker];
            var deviation = currentAllocation - targetAllocation;
            // Rebalancing amount based on total invested (cost basis)
            var rebalancingAmount = (targetAllocation / 100 * totalPortfolioValue) - totalInvested;

            // Only include if deviation is significant (>1%)
            if (Math.Abs(deviation) > 1m)
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

