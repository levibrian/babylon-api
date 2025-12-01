using System.Diagnostics;
using Babylon.Alfred.Api.Features.Investments.Analyzers;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Logging;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class PortfolioInsightsService(
    IPortfolioService portfolioService,
    ITransactionRepository transactionRepository,
    IEnumerable<IPortfolioAnalyzer> analyzers,
    ILogger<PortfolioInsightsService> logger)
    : IPortfolioInsightsService
{
    public async Task<List<PortfolioInsightDto>> GetTopInsightsAsync(Guid userId, int count = 3)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogOperationStart("GetTopInsights", new { UserId = userId, Count = count });

        // Fetch portfolio and transaction history
        var portfolio = await portfolioService.GetPortfolio(userId);
        var history = (await transactionRepository.GetAllByUser(userId)).ToList();

        // Run all analyzers in parallel
        var analyzerTasks = analyzers.Select(analyzer => analyzer.AnalyzeAsync(portfolio, history));
        var analyzerResults = await Task.WhenAll(analyzerTasks);

        // Flatten results and sort by severity (Critical > Warning > Info)
        var insights = analyzerResults
            .SelectMany(result => result)
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.Category == InsightCategory.Risk ? 1 : 0) // Prioritize risk insights
            .Take(count)
            .ToList();

        stopwatch.Stop();
        logger.LogPerformance("GetTopInsights", stopwatch.ElapsedMilliseconds, new { UserId = userId, InsightCount = insights.Count });
        logger.LogOperationSuccess("GetTopInsights", new { UserId = userId, Count = insights.Count });

        return insights;
    }
}

