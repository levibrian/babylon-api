namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Analytics;

/// <summary>
/// Risk metrics for portfolio analysis including volatility, beta, and Sharpe ratio.
/// Note: Requires historical price data to calculate.
/// </summary>
public class RiskMetricsDto
{
    /// <summary>
    /// Annualized volatility (standard deviation of returns).
    /// Calculated as: σ_annual = σ_daily * √252
    /// </summary>
    public decimal AnnualizedVolatility { get; set; }
    
    /// <summary>
    /// Beta coefficient vs benchmark (S&P 500).
    /// β = Covariance(Portfolio, Benchmark) / Variance(Benchmark)
    /// β > 1: More volatile than market
    /// β = 1: Same volatility as market
    /// β < 1: Less volatile than market
    /// </summary>
    public decimal Beta { get; set; }
    
    /// <summary>
    /// Sharpe Ratio: Risk-adjusted return.
    /// Sharpe = (Rp - Rf) / σp
    /// Higher is better (more return per unit of risk).
    /// </summary>
    public decimal SharpeRatio { get; set; }
    
    /// <summary>
    /// Annualized return of the portfolio (percentage).
    /// </summary>
    public decimal AnnualizedReturn { get; set; }
    
    /// <summary>
    /// Time period for calculation (e.g., "1Y", "3M", "6M").
    /// </summary>
    public string Period { get; set; } = "1Y";
    
    /// <summary>
    /// Benchmark ticker used for beta calculation.
    /// </summary>
    public string BenchmarkTicker { get; set; } = "^GSPC"; // S&P 500
}
