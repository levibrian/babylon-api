namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Interface for AI-powered rebalancing optimization (e.g., Gemini).
/// Implementations can reorder, adjust amounts, and generate explanations.
/// </summary>
public interface IRebalancingOptimizer
{
    /// <summary>
    /// Optimizes a set of rebalancing actions using AI.
    /// </summary>
    /// <param name="request">The optimization request with constraints and candidates.</param>
    /// <returns>Optimized actions with AI-generated explanations.</returns>
    Task<RebalancingOptimizerResponse> OptimizeAsync(RebalancingOptimizerRequest request);

    /// <summary>
    /// Whether the optimizer is enabled (feature flag check).
    /// </summary>
    bool IsEnabled { get; }
}

/// <summary>
/// Request payload for the rebalancing optimizer.
/// </summary>
public class RebalancingOptimizerRequest
{
    /// <summary>Constraints for the optimization.</summary>
    public RebalancingOptimizerConstraints Constraints { get; init; } = new();

    /// <summary>Securities with their features for analysis.</summary>
    public List<RebalancingOptimizerSecurity> Securities { get; init; } = [];

    /// <summary>Pre-classified sell candidates from deterministic logic.</summary>
    public List<RebalancingOptimizerCandidate> SellCandidates { get; init; } = [];

    /// <summary>Pre-classified buy candidates from deterministic logic.</summary>
    public List<RebalancingOptimizerCandidate> BuyCandidates { get; init; } = [];
}

public class RebalancingOptimizerConstraints
{
    /// <summary>Target net cashflow (ideally 0 for pure rebalancing).</summary>
    public decimal NetCashflowTarget { get; init; }

    /// <summary>Minimum action amount to include.</summary>
    public decimal NoiseThreshold { get; init; } = 10m;

    /// <summary>Maximum number of actions to return.</summary>
    public int MaxActions { get; init; } = 10;

    /// <summary>Sell percentile threshold (1Y) for timing.</summary>
    public decimal SellPercentileThreshold { get; init; } = 80m;

    /// <summary>Buy percentile threshold (1Y) for timing.</summary>
    public decimal BuyPercentileThreshold { get; init; } = 20m;

    /// <summary>Total portfolio value for context.</summary>
    public decimal TotalPortfolioValue { get; init; }

    /// <summary>Available cash (including new investment).</summary>
    public decimal CashAvailable { get; init; }
}

public class RebalancingOptimizerSecurity
{
    public string Ticker { get; init; } = string.Empty;
    public string SecurityName { get; init; } = string.Empty;
    public decimal CurrentAllocation { get; init; }
    public decimal TargetAllocation { get; init; }
    public decimal Deviation { get; init; }
    public decimal GapValue { get; init; }
    public decimal? CurrentPrice { get; init; }
    public decimal? Percentile1Y { get; init; }
    public decimal? UnrealizedPnLPercent { get; init; }
    public decimal? MarketValue { get; init; }
}

public class RebalancingOptimizerCandidate
{
    public string Ticker { get; init; } = string.Empty;
    public string ActionType { get; init; } = string.Empty; // "SELL" or "BUY"
    public decimal Amount { get; init; }
    public decimal Confidence { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Response from the rebalancing optimizer.
/// </summary>
public class RebalancingOptimizerResponse
{
    /// <summary>Whether the optimization was successful.</summary>
    public bool Success { get; init; }

    /// <summary>Error message if optimization failed.</summary>
    public string? Error { get; init; }

    /// <summary>Optimized actions.</summary>
    public List<RebalancingOptimizerAction> Actions { get; init; } = [];

    /// <summary>AI-generated summary of the recommendations.</summary>
    public string? Summary { get; init; }
}

public class RebalancingOptimizerAction
{
    public string Type { get; init; } = string.Empty; // "SELL" or "BUY"
    public string Ticker { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Reason { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
}
