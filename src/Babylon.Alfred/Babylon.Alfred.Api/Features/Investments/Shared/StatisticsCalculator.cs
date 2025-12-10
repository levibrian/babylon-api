namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Pure static utility class for statistical calculations.
/// All methods are deterministic with no side effects.
/// </summary>
public static class StatisticsCalculator
{
    /// <summary>
    /// Calculates daily log returns from a price series.
    /// Log return: r_t = ln(P_t / P_{t-1})
    /// </summary>
    /// <param name="prices">Dictionary of date to closing price</param>
    /// <returns>Dictionary mapping date to log return for that date</returns>
    public static Dictionary<DateTime, decimal> CalculateLogReturns(Dictionary<DateTime, decimal> prices)
    {
        var returns = new Dictionary<DateTime, decimal>();
        var sortedDates = prices.Keys.OrderBy(d => d).ToList();

        for (int i = 1; i < sortedDates.Count; i++)
        {
            var prevDate = sortedDates[i - 1];
            var currDate = sortedDates[i];

            if (prices.TryGetValue(prevDate, out var prevPrice) &&
                prices.TryGetValue(currDate, out var currPrice) &&
                prevPrice > 0)
            {
                var logReturn = (decimal)Math.Log((double)(currPrice / prevPrice));
                returns[currDate] = logReturn;
            }
        }

        return returns;
    }

    /// <summary>
    /// Calculates standard deviation of a value set.
    /// Uses population standard deviation (N divisor).
    /// </summary>
    public static decimal CalculateStandardDeviation(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0) return 0;

        var mean = values.Average();
        var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        return (decimal)Math.Sqrt((double)variance);
    }

    /// <summary>
    /// Calculates variance of a value set.
    /// Uses population variance (N divisor).
    /// </summary>
    public static decimal CalculateVariance(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0) return 0;

        var mean = values.Average();
        return values.Sum(v => (v - mean) * (v - mean)) / values.Count;
    }

    /// <summary>
    /// Calculates covariance between two value sets.
    /// Requires equal-length lists.
    /// </summary>
    public static decimal CalculateCovariance(IReadOnlyList<decimal> x, IReadOnlyList<decimal> y)
    {
        if (x.Count != y.Count || x.Count == 0) return 0;

        var xMean = x.Average();
        var yMean = y.Average();

        var covariance = x.Zip(y, (xi, yi) => (xi - xMean) * (yi - yMean)).Sum() / x.Count;
        return covariance;
    }

    /// <summary>
    /// Annualizes daily volatility using the square root of trading days per year.
    /// </summary>
    /// <param name="dailyVolatility">Standard deviation of daily returns</param>
    /// <param name="tradingDaysPerYear">Number of trading days (typically 252)</param>
    public static decimal AnnualizeVolatility(decimal dailyVolatility, int tradingDaysPerYear = 252)
    {
        return dailyVolatility * (decimal)Math.Sqrt(tradingDaysPerYear);
    }

    /// <summary>
    /// Calculates beta coefficient.
    /// β = Covariance(asset, benchmark) / Variance(benchmark)
    /// </summary>
    public static decimal CalculateBeta(IReadOnlyList<decimal> assetReturns, IReadOnlyList<decimal> benchmarkReturns)
    {
        var covariance = CalculateCovariance(assetReturns, benchmarkReturns);
        var benchmarkVariance = CalculateVariance(benchmarkReturns);
        return benchmarkVariance > 0 ? covariance / benchmarkVariance : 0;
    }

    /// <summary>
    /// Calculates Sharpe ratio.
    /// Sharpe = (Rp - Rf) / σp
    /// </summary>
    /// <param name="annualizedReturn">Annualized portfolio return</param>
    /// <param name="riskFreeRate">Risk-free rate (e.g., 0.03 for 3%)</param>
    /// <param name="annualizedVolatility">Annualized volatility (standard deviation)</param>
    public static decimal CalculateSharpeRatio(decimal annualizedReturn, decimal riskFreeRate, decimal annualizedVolatility)
    {
        return annualizedVolatility > 0
            ? (annualizedReturn - riskFreeRate) / annualizedVolatility
            : 0;
    }
}

