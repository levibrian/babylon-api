using System.Text.Json;

namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

public class PortfolioInsightDto
{
    public InsightCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedTicker { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public InsightSeverity Severity { get; set; }

    // Optional action properties
    public string? ActionLabel { get; set; }
    public JsonElement? ActionPayload { get; set; }
    public VisualContext? VisualContext { get; set; }
}

public class VisualContext
{
    public double CurrentValue { get; set; }
    public double TargetValue { get; set; }
    public double? ProjectedValue { get; set; }
    public VisualFormat Format { get; set; }
}

public enum VisualFormat
{
    Currency,
    Percent
}

public enum InsightCategory
{
    Risk,
    Opportunity,
    Trend,
    Efficiency,
    Income
}

public enum InsightSeverity
{
    Info,      // Gray
    Warning,   // Yellow
    Critical   // Red
}

