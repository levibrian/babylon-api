namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Rebalancing;

/// <summary>
/// Response DTO for timed rebalancing actions (with timing signals).
/// Includes concrete buy/sell actions filtered by simple timing signals (1Y percentile).
/// </summary>
public class TimedRebalancingActionsResponseDto
{
    public static TimedRebalancingActionsResponseDto Empty => new()
    {
        TotalPortfolioValue = 0,
        CashAvailable = 0,
        TotalBuyAmount = 0,
        TotalSellAmount = 0,
        NetCashFlow = 0,
        BuyPercentileThreshold1Y = 20,
        SellPercentileThreshold1Y = 80,
        GeneratedAtUtc = DateTime.UtcNow,
        Buys = [],
        Sells = []
    };

    /// <summary>Total portfolio value used for calculations (market value preferred).</summary>
    public decimal TotalPortfolioValue { get; init; }

    /// <summary>Cash available in the portfolio (if tracked).</summary>
    public decimal CashAvailable { get; init; }

    /// <summary>Total amount recommended to buy (currency).</summary>
    public decimal TotalBuyAmount { get; init; }

    /// <summary>Total amount recommended to sell (currency).</summary>
    public decimal TotalSellAmount { get; init; }

    /// <summary>Net cash flow (Buy - Sell). Ideally near zero unless cash is used.</summary>
    public decimal NetCashFlow { get; init; }

    /// <summary>Timing threshold for buys: percentile(1Y) at or below this is considered cheap.</summary>
    public decimal BuyPercentileThreshold1Y { get; init; }

    /// <summary>Timing threshold for sells: percentile(1Y) at or above this is considered expensive.</summary>
    public decimal SellPercentileThreshold1Y { get; init; }

    public DateTime GeneratedAtUtc { get; init; }

    public List<TimedRebalancingActionDto> Sells { get; init; } = [];
    public List<TimedRebalancingActionDto> Buys { get; init; } = [];

    /// <summary>Whether AI optimization was applied to this response.</summary>
    public bool AiApplied { get; init; }

    /// <summary>AI-generated summary (only present if AI optimization was used).</summary>
    public string? AiSummary { get; init; }
}

/// <summary>
/// A concrete timed rebalancing action for a security.
/// Amount is positive; direction is given by ActionType.
/// </summary>
public class TimedRebalancingActionDto
{
    public RebalancingActionType ActionType { get; init; }
    public string Ticker { get; init; } = string.Empty;
    public string SecurityName { get; init; } = string.Empty;

    /// <summary>Amount in currency (always positive).</summary>
    public decimal Amount { get; init; }

    /// <summary>Priority order (1 = highest priority). Only set when AI optimization is used.</summary>
    public int? Priority { get; init; }

    public decimal CurrentAllocationPercentage { get; init; }
    public decimal TargetAllocationPercentage { get; init; }
    public decimal AllocationDeviation { get; init; }

    public decimal? CurrentPrice { get; init; }
    public decimal? TimingPercentile1Y { get; init; }

    public decimal? UnrealizedPnLPercentage { get; init; }

    public string Reason { get; init; } = string.Empty;

    /// <summary>Confidence score 0.0-1.0. Higher = stronger signal.</summary>
    public decimal Confidence { get; init; }
}
