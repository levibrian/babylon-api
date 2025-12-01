using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Analyzers;

/// <summary>
/// Analyzes dividend history and forecasts expected income.
/// </summary>
public class IncomeAnalyzer : IPortfolioAnalyzer
{
    public Task<IEnumerable<PortfolioInsightDto>> AnalyzeAsync(PortfolioResponse portfolio, List<Transaction> history)
    {
        var insights = new List<PortfolioInsightDto>();

        // Run all income checks
        insights.AddRange(CheckDividendPatterns(history));

        return Task.FromResult<IEnumerable<PortfolioInsightDto>>(insights);
    }

    /// <summary>
    /// Checks dividend history for patterns and forecasts expected dividend payments.
    /// </summary>
    private static IEnumerable<PortfolioInsightDto> CheckDividendPatterns(List<Transaction> history)
    {
        var insights = new List<PortfolioInsightDto>();
        var now = DateTime.UtcNow;
        var currentMonth = now.Month;

        // Get all dividend transactions
        var dividendTransactions = history
            .Where(t => t.TransactionType == TransactionType.Dividend)
            .OrderByDescending(t => t.Date)
            .ToList();

        if (dividendTransactions.Count == 0)
        {
            return insights;
        }

        // Group dividends by security and find patterns
        var dividendGroups = dividendTransactions
            .GroupBy(t => t.SecurityId)
            .ToList();

        foreach (var group in dividendGroups)
        {
            var firstTransaction = group.First();
            var security = firstTransaction.Security;

            if (security == null)
            {
                continue;
            }

            // Check if this security has dividends in the current month from previous years
            var dividendsInCurrentMonth = group
                .Where(t => t.Date.Month == currentMonth && t.Date.Year < now.Year)
                .OrderByDescending(t => t.Date)
                .ToList();

            if (dividendsInCurrentMonth.Count > 0)
            {
                var insight = CreateDividendInsight(security, group, dividendsInCurrentMonth, now, currentMonth);
                if (insight != null)
                {
                    insights.Add(insight);
                }
            }
        }

        return insights;
    }

    /// <summary>
    /// Creates a dividend insight if the expected dividend date is within 30 days.
    /// </summary>
    private static PortfolioInsightDto? CreateDividendInsight(
        Babylon.Alfred.Api.Shared.Data.Models.Security security,
        IGrouping<Guid, Transaction> dividendGroup,
        List<Transaction> dividendsInCurrentMonth,
        DateTime now,
        int currentMonth)
    {
        // Found historical pattern - this security pays dividends in this month
        var mostRecentDividend = dividendsInCurrentMonth.First();
        var averageDividendPerShare = dividendGroup.Average(t => t.SharePrice);
        var averageShares = dividendGroup.Average(t => t.SharesQuantity);
        var estimatedAmount = averageDividendPerShare * averageShares;

        // Check if we're within 30 days of when the dividend typically occurs
        var expectedDate = new DateTime(now.Year, currentMonth, mostRecentDividend.Date.Day);
        var daysUntilExpected = (expectedDate - now).TotalDays;

        if (daysUntilExpected < -30 || daysUntilExpected > 30)
        {
            return null; // Too far from expected date
        }

        return new PortfolioInsightDto
        {
            Category = InsightCategory.Income,
            Title = "Dividend Season",
            Message = $"{security.SecurityName} usually pays dividends in {now:MMMM}. Est: €{estimatedAmount:F2}.",
            RelatedTicker = security.Ticker,
            Severity = InsightSeverity.Info,
            Metadata = new Dictionary<string, object>
            {
                { "expectedDate", expectedDate.ToString("yyyy-MM-dd") },
                { "estimatedAmount", estimatedAmount },
                { "dividendPerShare", averageDividendPerShare },
                { "historicalCount", dividendsInCurrentMonth.Count }
            },
            VisualContext = new VisualContext
            {
                CurrentValue = (double)estimatedAmount,
                Format = VisualFormat.Currency
            },
            ActionLabel = "Log Receipt",
            ActionPayload = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                ticker = security.Ticker,
                type = "dividend",
                expectedDate = expectedDate.ToString("yyyy-MM-dd")
            })
        };
    }
}

