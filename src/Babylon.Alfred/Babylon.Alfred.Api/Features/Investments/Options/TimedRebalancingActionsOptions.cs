namespace Babylon.Alfred.Api.Features.Investments.Options;

/// <summary>
/// Options for timed rebalancing actions (with timing signals).
/// </summary>
public class TimedRebalancingActionsOptions
{
    public const string SectionName = "Rebalancing:TimedActions";

    /// <summary>Timing threshold for buys: percentile(1Y) at or below this is considered cheap.</summary>
    public decimal BuyPercentileThreshold1Y { get; init; } = 20m;

    /// <summary>Timing threshold for sells: percentile(1Y) at or above this is considered expensive.</summary>
    public decimal SellPercentileThreshold1Y { get; init; } = 80m;

    /// <summary>Minimum absolute action amount (currency) to include (noise filter).</summary>
    public decimal NoiseThreshold { get; init; } = 10m;

    /// <summary>Maximum number of tickers to fetch historical data for per request (rate-limit protection).</summary>
    public int MaxTickersForTiming { get; init; } = 15;

    /// <summary>Default maximum number of actions returned (if maxActions is not specified).</summary>
    public int DefaultMaxActions { get; init; } = 10;
}
