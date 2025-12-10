namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Analytics;

/// <summary>
/// Diversification metrics for portfolio analysis using Herfindahl-Hirschman Index (HHI).
/// </summary>
public class DiversificationMetricsDto
{
    /// <summary>
    /// Returns an empty diversification metrics instance for portfolios with no positions.
    /// </summary>
    public static DiversificationMetricsDto Empty => new()
    {
        HHI = 0,
        EffectiveN = 0,
        DiversificationScore = 0,
        Top3Concentration = 0,
        Top5Concentration = 0,
        TotalAssets = 0
    };

    /// <summary>
    /// Herfindahl-Hirschman Index: Sum of squared weights. Range: 0-1. Lower is better.
    /// HHI &lt; 0.15 indicates good diversification.
    /// </summary>
    public decimal HHI { get; init; }

    /// <summary>
    /// Effective Number of Bets: 1 / HHI. Represents equivalent number of equally-weighted positions.
    /// Higher is better (more diversified).
    /// </summary>
    public decimal EffectiveN { get; init; }

    /// <summary>
    /// Diversification Score: (1 - HHI) * 100. Range: 0-100. Higher is better.
    /// </summary>
    public decimal DiversificationScore { get; init; }

    /// <summary>
    /// Percentage of total portfolio value in top 3 holdings.
    /// </summary>
    public decimal Top3Concentration { get; init; }

    /// <summary>
    /// Percentage of total portfolio value in top 5 holdings.
    /// </summary>
    public decimal Top5Concentration { get; init; }

    /// <summary>
    /// Total number of distinct securities in the portfolio.
    /// </summary>
    public int TotalAssets { get; init; }
}
