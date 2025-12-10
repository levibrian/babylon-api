using Babylon.Alfred.Api.Features.Investments.Models.Responses.Analytics;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;

namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Service for calculating portfolio analytics including diversification and risk metrics.
/// </summary>
public class PortfolioAnalyticsService(
    IPortfolioService portfolioService,
    IHistoricalPriceService historicalPriceService) : IPortfolioAnalyticsService
{
    /// <summary>
    /// Calculates diversification metrics using Herfindahl-Hirschman Index (HHI).
    /// </summary>
    /// <remarks>
    /// Algorithm:
    /// <list type="number">
    /// <item>HHI = Σ(wi²) where wi is the weight of asset i</item>
    /// <item>Effective N = 1 / HHI (number of equivalent equally-weighted positions)</item>
    /// <item>Diversification Score = (1 - HHI) * 100 (0-100 scale)</item>
    /// </list>
    /// </remarks>
    public async Task<DiversificationMetricsDto> GetDiversificationMetricsAsync(Guid userId)
    {
        var portfolio = await portfolioService.GetPortfolio(userId);
        var totalValue = portfolio.Positions.Sum(p => p.CurrentMarketValue ?? 0);
        
        if (totalValue == 0 || portfolio.Positions.Count == 0)
        {
            return DiversificationMetricsDto.Empty;
        }

        var weights = portfolio.Positions
            .Select(p => (p.CurrentMarketValue ?? 0) / totalValue)
            .Where(w => w > 0)
            .ToList();
        
        var hhi = weights.Sum(w => w * w);
        var effectiveN = hhi > 0 ? 1m / hhi : 0;
        var score = (1m - hhi) * 100;
        
        var sortedWeights = weights.OrderByDescending(w => w).ToList();
        
        return new DiversificationMetricsDto
        {
            HHI = Math.Round(hhi, 4),
            EffectiveN = Math.Round(effectiveN, 2),
            DiversificationScore = Math.Round(score, 2),
            Top3Concentration = Math.Round(sortedWeights.Take(3).Sum() * 100, 2),
            Top5Concentration = Math.Round(sortedWeights.Take(5).Sum() * 100, 2),
            TotalAssets = portfolio.Positions.Count
        };
    }
    
    /// <summary>
    /// Calculates risk metrics including volatility, beta, and Sharpe ratio.
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="period">Time period: 1Y, 6M, or 3M</param>
    /// <exception cref="ArgumentException">Thrown when period is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when insufficient data is available</exception>
    public async Task<RiskMetricsDto> GetRiskMetricsAsync(Guid userId, string period = "1Y")
    {
        ValidatePeriod(period);

        var portfolio = await portfolioService.GetPortfolio(userId);

        if (portfolio.Positions.Count == 0)
        {
            return RiskMetricsDto.Empty(period);
        }

        var dateRange = CalculateDateRange(period);
        var positionPrices = await FetchPositionPricesAsync(portfolio.Positions, dateRange);
        var benchmarkPrices = await FetchBenchmarkPricesAsync(dateRange);

        var portfolioDailyValues = CalculatePortfolioDailyValues(portfolio.Positions, positionPrices);

        ValidateDataSufficiency(portfolioDailyValues, benchmarkPrices);

        var (portfolioReturns, benchmarkReturns) = CalculateAlignedReturns(portfolioDailyValues, benchmarkPrices);

        return CalculateRiskMetrics(portfolioReturns, benchmarkReturns, period);
    }

    #region Private Helpers

    private static void ValidatePeriod(string period)
    {
        var validPeriods = new[] { RiskMetricsConstants.Periods.OneYear, RiskMetricsConstants.Periods.SixMonths, RiskMetricsConstants.Periods.ThreeMonths };
        if (!validPeriods.Contains(period))
        {
            throw new ArgumentException(
                $"Invalid period '{period}'. Valid values are: {string.Join(", ", validPeriods)}",
                nameof(period));
        }
    }

    private static (DateTime StartDate, DateTime EndDate) CalculateDateRange(string period)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = period switch
        {
            RiskMetricsConstants.Periods.OneYear => endDate.AddYears(-1),
            RiskMetricsConstants.Periods.SixMonths => endDate.AddMonths(-6),
            RiskMetricsConstants.Periods.ThreeMonths => endDate.AddMonths(-3),
            _ => endDate.AddYears(-1)
        };
        return (startDate, endDate);
    }

    private async Task<Dictionary<string, Dictionary<DateTime, decimal>>> FetchPositionPricesAsync(
        IEnumerable<PortfolioPositionDto> positions,
        (DateTime StartDate, DateTime EndDate) dateRange)
    {
        var positionPrices = new Dictionary<string, Dictionary<DateTime, decimal>>();

        foreach (var position in positions)
        {
            var prices = await historicalPriceService.GetHistoricalPricesAsync(
                position.Ticker, dateRange.StartDate, dateRange.EndDate);

            if (prices.Count > 0)
            {
                positionPrices[position.Ticker] = prices;
            }
        }

        if (positionPrices.Count == 0)
        {
            throw new InvalidOperationException("No historical price data available for portfolio positions.");
        }

        return positionPrices;
    }

    private async Task<Dictionary<DateTime, decimal>> FetchBenchmarkPricesAsync(
        (DateTime StartDate, DateTime EndDate) dateRange)
    {
        var benchmarkPrices = await historicalPriceService.GetHistoricalPricesAsync(
            RiskMetricsConstants.DefaultBenchmarkTicker,
            dateRange.StartDate,
            dateRange.EndDate);

        if (benchmarkPrices.Count == 0)
        {
            throw new InvalidOperationException(
                $"No historical price data available for benchmark {RiskMetricsConstants.DefaultBenchmarkTicker}.");
        }

        return benchmarkPrices;
    }

    private static Dictionary<DateTime, decimal> CalculatePortfolioDailyValues(
        IEnumerable<PortfolioPositionDto> positions,
        Dictionary<string, Dictionary<DateTime, decimal>> positionPrices)
    {
        var allDates = positionPrices.Values
            .SelectMany(p => p.Keys)
            .Distinct()
            .OrderBy(d => d);

        var portfolioDailyValues = new Dictionary<DateTime, decimal>();
        var positionsList = positions.ToList();

        foreach (var date in allDates)
        {
            var portfolioValue = CalculatePortfolioValueForDate(positionsList, positionPrices, date);
            if (portfolioValue.HasValue)
            {
                portfolioDailyValues[date] = portfolioValue.Value;
            }
        }

        return portfolioDailyValues;
    }

    private static decimal? CalculatePortfolioValueForDate(
        List<PortfolioPositionDto> positions,
        Dictionary<string, Dictionary<DateTime, decimal>> positionPrices,
        DateTime date)
    {
        decimal portfolioValue = 0;

        foreach (var position in positions)
        {
            if (positionPrices.TryGetValue(position.Ticker, out var prices) &&
                prices.TryGetValue(date, out var price))
            {
                portfolioValue += position.TotalShares * price;
            }
            else
            {
                return null; // Missing data for this date
            }
        }

        return portfolioValue > 0 ? portfolioValue : null;
    }

    private static void ValidateDataSufficiency(
        Dictionary<DateTime, decimal> portfolioDailyValues,
        Dictionary<DateTime, decimal> benchmarkPrices)
    {
        if (portfolioDailyValues.Count < RiskMetricsConstants.MinimumDataPoints)
        {
            throw new InvalidOperationException(
                $"Insufficient portfolio data for risk calculation (need at least {RiskMetricsConstants.MinimumDataPoints} data points).");
        }

        if (benchmarkPrices.Count < RiskMetricsConstants.MinimumDataPoints)
        {
            throw new InvalidOperationException(
                $"Insufficient benchmark data for risk calculation (need at least {RiskMetricsConstants.MinimumDataPoints} data points).");
        }
    }

    private static (List<decimal> PortfolioReturns, List<decimal> BenchmarkReturns) CalculateAlignedReturns(
        Dictionary<DateTime, decimal> portfolioDailyValues,
        Dictionary<DateTime, decimal> benchmarkPrices)
    {
        var portfolioReturnsDict = StatisticsCalculator.CalculateLogReturns(portfolioDailyValues);
        var benchmarkReturnsDict = StatisticsCalculator.CalculateLogReturns(benchmarkPrices);

        if (portfolioReturnsDict.Count == 0 || benchmarkReturnsDict.Count == 0)
        {
            throw new InvalidOperationException("Unable to calculate returns from historical prices.");
        }

        var commonDates = portfolioReturnsDict.Keys
            .Intersect(benchmarkReturnsDict.Keys)
            .OrderBy(d => d)
            .ToList();

        if (commonDates.Count < RiskMetricsConstants.MinimumDataPoints)
        {
            throw new InvalidOperationException(
                $"Insufficient aligned return data for risk calculation (need at least {RiskMetricsConstants.MinimumDataPoints} data points).");
        }

        var portfolioReturns = commonDates.Select(d => portfolioReturnsDict[d]).ToList();
        var benchmarkReturns = commonDates.Select(d => benchmarkReturnsDict[d]).ToList();

        return (portfolioReturns, benchmarkReturns);
    }

    private static RiskMetricsDto CalculateRiskMetrics(
        List<decimal> portfolioReturns,
        List<decimal> benchmarkReturns,
        string period)
    {
        var dailyVolatility = StatisticsCalculator.CalculateStandardDeviation(portfolioReturns);
        var annualizedVolatility = StatisticsCalculator.AnnualizeVolatility(dailyVolatility, RiskMetricsConstants.TradingDaysPerYear);
        var beta = StatisticsCalculator.CalculateBeta(portfolioReturns, benchmarkReturns);

        var totalReturn = portfolioReturns.Sum();
        var annualizedReturn = totalReturn * (RiskMetricsConstants.TradingDaysPerYear / (decimal)portfolioReturns.Count);

        var sharpeRatio = StatisticsCalculator.CalculateSharpeRatio(
            annualizedReturn,
            RiskMetricsConstants.RiskFreeRate,
            annualizedVolatility);

        return new RiskMetricsDto
        {
            AnnualizedVolatility = Math.Round(annualizedVolatility, 4),
            Beta = Math.Round(beta, 4),
            SharpeRatio = Math.Round(sharpeRatio, 4),
            AnnualizedReturn = Math.Round(annualizedReturn, 4),
            Period = period,
            BenchmarkTicker = RiskMetricsConstants.DefaultBenchmarkTicker
        };
    }

    #endregion
}
