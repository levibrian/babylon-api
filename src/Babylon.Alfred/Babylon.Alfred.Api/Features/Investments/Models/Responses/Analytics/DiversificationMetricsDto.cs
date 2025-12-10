using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Analytics;

/// <summary>
/// Diversification metrics for portfolio analysis using Herfindahl-Hirschman Index (HHI).
/// </summary>
public class DiversificationMetricsDto
{
    /// <summary>
    /// Herfindahl-Hirschman Index: Sum of squared weights. Range: 0-1. Lower is better.
    /// HHI < 0.15 indicates good diversification.
    /// </summary>
    public decimal HHI { get; set; }
    
    /// <summary>
    /// Effective Number of Bets: 1 / HHI. Represents equivalent number of equally-weighted positions.
    /// Higher is better (more diversified).
    /// </summary>
    public decimal EffectiveN { get; set; }
    
    /// <summary>
    /// Diversification Score: (1 - HHI) * 100. Range: 0-100. Higher is better.
    /// </summary>
    public decimal DiversificationScore { get; set; }
    
    /// <summary>
    /// Percentage of total portfolio value in top 3 holdings.
    /// </summary>
    public decimal Top3Concentration { get; set; }
    
    /// <summary>
    /// Percentage of total portfolio value in top 5 holdings.
    /// </summary>
    public decimal Top5Concentration { get; set; }
    
    /// <summary>
    /// Total number of distinct securities in the portfolio.
    /// </summary>
    public int TotalAssets { get; set; }
}
