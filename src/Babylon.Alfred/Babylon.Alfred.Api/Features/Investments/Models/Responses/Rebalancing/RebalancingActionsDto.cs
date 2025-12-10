namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Rebalancing;

/// <summary>
/// Response containing all rebalancing actions for a portfolio.
/// </summary>
public class RebalancingActionsDto
{
    /// <summary>
    /// Returns an empty response for portfolios with no rebalancing needed.
    /// </summary>
    public static RebalancingActionsDto Empty => new()
    {
        TotalPortfolioValue = 0,
        Actions = new List<RebalancingActionDto>()
    };

    /// <summary>
    /// List of rebalancing actions (buy/sell recommendations).
    /// </summary>
    public List<RebalancingActionDto> Actions { get; init; } = new();

    /// <summary>
    /// Total portfolio value used for calculations.
    /// </summary>
    public decimal TotalPortfolioValue { get; init; }

    /// <summary>
    /// Total amount to buy (sum of all positive difference values).
    /// </summary>
    public decimal TotalBuyAmount { get; init; }

    /// <summary>
    /// Total amount to sell (sum of all negative difference values, as absolute value).
    /// </summary>
    public decimal TotalSellAmount { get; init; }

    /// <summary>
    /// Net cash flow (should be approximately zero for pure rebalancing).
    /// </summary>
    public decimal NetCashFlow { get; init; }
}

