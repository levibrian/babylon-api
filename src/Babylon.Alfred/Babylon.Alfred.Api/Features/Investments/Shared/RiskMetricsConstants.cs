namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Constants for risk metrics calculations.
/// </summary>
public static class RiskMetricsConstants
{
    /// <summary>
    /// S&P 500 ticker symbol used as the default benchmark.
    /// </summary>
    public const string DefaultBenchmarkTicker = "^GSPC";

    /// <summary>
    /// Risk-free rate for Sharpe ratio calculation.
    /// Using US Treasury rate approximation.
    /// </summary>
    public const decimal RiskFreeRate = 0.03m; // 3%

    /// <summary>
    /// Number of trading days per year for annualization.
    /// Standard assumption for US equity markets.
    /// </summary>
    public const int TradingDaysPerYear = 252;

    /// <summary>
    /// Minimum data points required for statistical calculations.
    /// </summary>
    public const int MinimumDataPoints = 2;

    /// <summary>
    /// Supported period values for risk calculations.
    /// </summary>
    public static class Periods
    {
        public const string OneYear = "1Y";
        public const string SixMonths = "6M";
        public const string ThreeMonths = "3M";
    }
}

