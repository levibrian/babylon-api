using Babylon.Alfred.Api.Features.Investments.Shared;

namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Analytics;

/// <summary>
/// Risk metrics for portfolio analysis including volatility, beta, and Sharpe ratio.
/// </summary>
public class RiskMetricsDto
{
    /// <summary>
    /// Returns an empty risk metrics instance for portfolios with no positions.
    /// </summary>
    public static RiskMetricsDto Empty(string period = "1Y") => new()
    {
        AnnualizedVolatility = 0,
        Beta = 0,
        SharpeRatio = 0,
        AnnualizedReturn = 0,
        Period = period,
        BenchmarkTicker = RiskMetricsConstants.DefaultBenchmarkTicker
    };

    /// <summary>
    /// Annualized volatility (standard deviation of returns).
    /// Calculated as: σ_annual = σ_daily * √252
    /// </summary>
    public decimal AnnualizedVolatility { get; init; }

    /// <summary>
    /// Beta coefficient vs benchmark (S&amp;P 500).
    /// β &gt; 1: More volatile than market.
    /// β = 1: Same volatility as market.
    /// β &lt; 1: Less volatile than market.
    /// </summary>
    public decimal Beta { get; init; }

    /// <summary>
    /// Sharpe Ratio: Risk-adjusted return.
    /// Higher is better (more return per unit of risk).
    /// </summary>
    public decimal SharpeRatio { get; init; }

    /// <summary>
    /// Annualized return of the portfolio.
    /// </summary>
    public decimal AnnualizedReturn { get; init; }

    /// <summary>
    /// Time period for calculation (e.g., "1Y", "3M", "6M").
    /// </summary>
    public string Period { get; init; } = "1Y";

    /// <summary>
    /// Benchmark ticker used for beta calculation.
    /// </summary>
    public string BenchmarkTicker { get; init; } = RiskMetricsConstants.DefaultBenchmarkTicker;
}
