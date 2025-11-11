namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

public class PortfolioInsightDto
{
    public InsightType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public decimal? Amount { get; set; }
    public InsightSeverity Severity { get; set; }
}

public enum InsightType
{
    Rebalancing,
    PerformanceMilestone,
    DiversificationWarning
    // Future: CriticalWarning, etc.
}

public enum InsightSeverity
{
    Info,
    Warning,
    Critical
}

