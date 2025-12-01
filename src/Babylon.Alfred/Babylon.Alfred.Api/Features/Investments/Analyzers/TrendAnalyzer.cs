using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Analyzers;

/// <summary>
/// Analyzes portfolio trends and momentum patterns.
/// </summary>
public class TrendAnalyzer(
    IMarketPriceService marketPriceService)
    : IPortfolioAnalyzer
{
    public async Task<IEnumerable<PortfolioInsightDto>> AnalyzeAsync(PortfolioResponse portfolio, List<Transaction> history)
    {
        var insights = new List<PortfolioInsightDto>();

        if (portfolio.Positions.Count == 0)
        {
            return insights;
        }

        // Get current market prices
        var tickers = portfolio.Positions.Select(p => p.Ticker).ToList();
        var currentPrices = await marketPriceService.GetCurrentPricesAsync(tickers);

        // Run all trend checks
        insights.AddRange(CheckMomentumWinners(portfolio, currentPrices));
        insights.AddRange(CheckDrawdownLosers(portfolio, currentPrices));

        return insights;
    }

    /// <summary>
    /// Checks for momentum winners: assets that are up significantly from average cost.
    /// </summary>
    private static IEnumerable<PortfolioInsightDto> CheckMomentumWinners(
        PortfolioResponse portfolio,
        Dictionary<string, decimal> currentPrices)
    {
        var insights = new List<PortfolioInsightDto>();
        const decimal winnerThreshold = 20m; // 20% gain threshold
        const decimal extremeWinnerThreshold = 50m; // 50% gain for warning severity

        foreach (var position in portfolio.Positions)
        {
            if (position.AverageSharePrice == 0 || !currentPrices.TryGetValue(position.Ticker, out var currentPrice))
            {
                continue;
            }

            var percentageChange = CalculatePercentageChange(position.AverageSharePrice, currentPrice);

            if (percentageChange > winnerThreshold)
            {
                var priceChange = currentPrice - position.AverageSharePrice;
                insights.Add(new PortfolioInsightDto
                {
                    Category = InsightCategory.Trend,
                    Title = "Momentum Alert",
                    Message = $"{position.SecurityName} is rallying. Up {percentageChange:F1}% from your average cost.",
                    RelatedTicker = position.Ticker,
                    Severity = percentageChange > extremeWinnerThreshold ? InsightSeverity.Warning : InsightSeverity.Info,
                    Metadata = new Dictionary<string, object>
                    {
                        { "percentageChange", percentageChange },
                        { "averageCost", position.AverageSharePrice },
                        { "currentPrice", currentPrice },
                        { "totalShares", position.TotalShares },
                        { "unrealizedGain", priceChange * position.TotalShares }
                    },
                    VisualContext = new VisualContext
                    {
                        CurrentValue = (double)currentPrice,
                        TargetValue = (double)position.AverageSharePrice,
                        Format = VisualFormat.Currency
                    }
                });
            }
        }

        return insights;
    }

    /// <summary>
    /// Checks for drawdown losers: assets that are down significantly from average cost.
    /// </summary>
    private static IEnumerable<PortfolioInsightDto> CheckDrawdownLosers(
        PortfolioResponse portfolio,
        Dictionary<string, decimal> currentPrices)
    {
        var insights = new List<PortfolioInsightDto>();
        const decimal loserThreshold = -15m; // 15% loss threshold
        const decimal extremeLoserThreshold = -30m; // 30% loss for critical severity

        foreach (var position in portfolio.Positions)
        {
            if (position.AverageSharePrice == 0 || !currentPrices.TryGetValue(position.Ticker, out var currentPrice))
            {
                continue;
            }

            var percentageChange = CalculatePercentageChange(position.AverageSharePrice, currentPrice);

            if (percentageChange < loserThreshold)
            {
                var priceChange = currentPrice - position.AverageSharePrice;
                insights.Add(new PortfolioInsightDto
                {
                    Category = InsightCategory.Risk,
                    Title = "Drawdown Alert",
                    Message = $"{position.SecurityName} is in correction. Down {Math.Abs(percentageChange):F1}% from your average cost.",
                    RelatedTicker = position.Ticker,
                    Severity = percentageChange < extremeLoserThreshold ? InsightSeverity.Critical : InsightSeverity.Warning,
                    Metadata = new Dictionary<string, object>
                    {
                        { "percentageChange", percentageChange },
                        { "averageCost", position.AverageSharePrice },
                        { "currentPrice", currentPrice },
                        { "totalShares", position.TotalShares },
                        { "unrealizedLoss", Math.Abs(priceChange * position.TotalShares) }
                    },
                    VisualContext = new VisualContext
                    {
                        CurrentValue = (double)currentPrice,
                        TargetValue = (double)position.AverageSharePrice,
                        Format = VisualFormat.Currency
                    }
                });
            }
        }

        return insights;
    }

    /// <summary>
    /// Calculates the percentage change from average cost to current price.
    /// </summary>
    private static decimal CalculatePercentageChange(decimal averageCost, decimal currentPrice)
    {
        if (averageCost == 0)
        {
            return 0;
        }

        var priceChange = currentPrice - averageCost;
        return (priceChange / averageCost) * 100;
    }
}

