namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Rebalancing;

/// <summary>
/// Represents a single rebalancing action (buy or sell) for a security.
/// </summary>
public class RebalancingActionDto
{
    /// <summary>
    /// Ticker symbol of the security.
    /// </summary>
    public string Ticker { get; init; } = string.Empty;

    /// <summary>
    /// Security name.
    /// </summary>
    public string SecurityName { get; init; } = string.Empty;

    /// <summary>
    /// Current allocation percentage.
    /// </summary>
    public decimal CurrentAllocationPercentage { get; init; }

    /// <summary>
    /// Target allocation percentage.
    /// </summary>
    public decimal TargetAllocationPercentage { get; init; }

    /// <summary>
    /// Difference value in currency (positive = buy, negative = sell).
    /// </summary>
    public decimal DifferenceValue { get; init; }

    /// <summary>
    /// Action type: Buy or Sell.
    /// </summary>
    public RebalancingActionType ActionType { get; init; }
}

/// <summary>
/// Type of rebalancing action.
/// </summary>
public enum RebalancingActionType
{
    /// <summary>Buy to increase allocation.</summary>
    Buy,
    /// <summary>Sell to decrease allocation.</summary>
    Sell
}

