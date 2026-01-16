using System.Globalization;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Analyzers;

/// <summary>
/// Analyzes portfolio structural health and risk factors.
/// </summary>
public class RiskAnalyzer : IPortfolioAnalyzer
{
    public Task<IEnumerable<PortfolioInsightDto>> AnalyzeAsync(PortfolioResponse portfolio, List<Transaction> history)
    {
        var insights = new List<PortfolioInsightDto>();

        if (portfolio.Positions.Count == 0 || portfolio.TotalInvested == 0)
        {
            return Task.FromResult<IEnumerable<PortfolioInsightDto>>(insights);
        }

        // Run all risk checks
        insights.AddRange(CheckConcentrationRisk(portfolio));
        insights.AddRange(CheckDiversification(portfolio));
        insights.AddRange(CheckSectorExposure(portfolio, history));

        return Task.FromResult<IEnumerable<PortfolioInsightDto>>(insights);
    }

    /// <summary>
    /// Checks for concentration risk: flags assets that exceed 20% of portfolio value.
    /// </summary>
    private static IEnumerable<PortfolioInsightDto> CheckConcentrationRisk(PortfolioResponse portfolio)
    {
        var insights = new List<PortfolioInsightDto>();
        var totalInvested = portfolio.TotalInvested;

        foreach (var position in portfolio.Positions)
        {
            var allocationPercentage = totalInvested > 0
                ? (position.TotalInvested / totalInvested) * 100
                : 0;

            if (allocationPercentage > 20m)
            {
                insights.Add(new PortfolioInsightDto
                {
                    Category = InsightCategory.Risk,
                    Title = "Concentration Risk",
                    Message = $"{position.SecurityName} makes up {allocationPercentage.ToString("F1", CultureInfo.InvariantCulture)}% of your portfolio.",
                    RelatedTicker = position.Ticker,
                    Severity = allocationPercentage > 40m ? InsightSeverity.Critical : InsightSeverity.Warning,
                    Metadata = new Dictionary<string, object>
                    {
                        { "allocationPercentage", allocationPercentage },
                        { "totalInvested", position.TotalInvested },
                        { "ticker", position.Ticker }
                    },
                    VisualContext = new VisualContext
                    {
                        CurrentValue = (double)allocationPercentage,
                        TargetValue = 20.0,
                        Format = VisualFormat.Percent
                    }
                });
            }
        }

        return insights;
    }

    /// <summary>
    /// Checks portfolio diversification: warns if asset count is below recommended minimum.
    /// </summary>
    private static IEnumerable<PortfolioInsightDto> CheckDiversification(PortfolioResponse portfolio)
    {
        var insights = new List<PortfolioInsightDto>();
        var assetCount = portfolio.Positions.Count;
        const int recommendedMinimum = 5;

        if (assetCount < recommendedMinimum)
        {
            insights.Add(new PortfolioInsightDto
            {
                Category = InsightCategory.Risk,
                Title = "Low Diversification",
                Message = $"Your portfolio contains only {assetCount} asset{(assetCount == 1 ? "" : "s")}. Consider diversifying across more holdings.",
                Severity = assetCount < 3 ? InsightSeverity.Warning : InsightSeverity.Info,
                Metadata = new Dictionary<string, object>
                {
                    { "assetCount", assetCount },
                    { "recommendedMinimum", recommendedMinimum }
                }
            });
        }

        return insights;
    }

    /// <summary>
    /// Checks sector exposure: warns if portfolio is over-concentrated in a single sector.
    /// Note: Requires Sector property on Security model - currently a placeholder.
    /// </summary>
    private static IEnumerable<PortfolioInsightDto> CheckSectorExposure(PortfolioResponse portfolio, List<Transaction> history)
    {
        // TODO: Implement when Security model includes Sector property
        // Logic: Group positions by sector, calculate sector allocation percentages
        // Flag if any single sector > 50% of portfolio value
        return Enumerable.Empty<PortfolioInsightDto>();
    }
}

