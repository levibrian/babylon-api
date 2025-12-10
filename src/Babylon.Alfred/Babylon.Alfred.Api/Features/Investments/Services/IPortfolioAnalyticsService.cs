using Babylon.Alfred.Api.Features.Investments.Models.Responses.Analytics;

namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Service for calculating portfolio analytics including diversification and risk metrics.
/// </summary>
public interface IPortfolioAnalyticsService
{
    /// <summary>
    /// Calculates diversification metrics using Herfindahl-Hirschman Index (HHI).
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <returns>Diversification metrics including HHI, Effective N, and concentration</returns>
    Task<DiversificationMetricsDto> GetDiversificationMetricsAsync(Guid userId);
    
    /// <summary>
    /// Calculates risk metrics including volatility, beta, and Sharpe ratio.
    /// Requires historical price data.
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="period">Time period for analysis (1Y, 3M, 6M, etc.)</param>
    /// <returns>Risk metrics</returns>
    Task<RiskMetricsDto> GetRiskMetricsAsync(Guid userId, string period = "1Y");
}
