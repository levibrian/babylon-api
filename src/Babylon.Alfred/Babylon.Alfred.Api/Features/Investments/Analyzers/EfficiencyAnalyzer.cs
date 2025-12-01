using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Analyzers;

/// <summary>
/// Analyzes portfolio efficiency and identifies cash drag or dead assets.
/// </summary>
public class EfficiencyAnalyzer : IPortfolioAnalyzer
{
    public Task<IEnumerable<PortfolioInsightDto>> AnalyzeAsync(PortfolioResponse portfolio, List<Transaction> history)
    {
        var insights = new List<PortfolioInsightDto>();

        if (portfolio.Positions.Count == 0)
        {
            return Task.FromResult<IEnumerable<PortfolioInsightDto>>(insights);
        }

        // Run all efficiency checks
        insights.AddRange(CheckDeadAssets(portfolio, history));
        insights.AddRange(CheckCashDrag(portfolio, history));

        return Task.FromResult<IEnumerable<PortfolioInsightDto>>(insights);
    }

    /// <summary>
    /// Checks for dead assets: positions with minimal or no growth over time.
    /// Note: Requires market price data - currently a placeholder for future implementation.
    /// </summary>
    private static IEnumerable<PortfolioInsightDto> CheckDeadAssets(PortfolioResponse portfolio, List<Transaction> history)
    {
        // TODO: Implement when market price data is consistently available
        // Logic:
        // 1. Compare current market value vs cost basis for each position
        // 2. Flag positions with 0% or negative growth over 6+ months
        // 3. Consider transaction activity as a signal
        return Enumerable.Empty<PortfolioInsightDto>();
    }

    /// <summary>
    /// Checks for cash drag: uninvested cash that could be deployed.
    /// Note: Requires cash balance tracking - currently a placeholder for future implementation.
    /// </summary>
    private static IEnumerable<PortfolioInsightDto> CheckCashDrag(PortfolioResponse portfolio, List<Transaction> history)
    {
        // TODO: Implement when cash balance tracking is added
        // Logic:
        // 1. Compare TotalInvested vs NetWorth (if tracked)
        // 2. Flag if cash balance > 10% of portfolio value
        // 3. Suggest deploying cash into target allocations
        return Enumerable.Empty<PortfolioInsightDto>();
    }
}

