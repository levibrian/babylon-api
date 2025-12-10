using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Rebalancing;

namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Service interface for calculating rebalancing actions and smart recommendations.
/// </summary>
public interface IRebalancingService
{
    /// <summary>
    /// Calculates rebalancing actions for all positions in the portfolio.
    /// Pure rebalancing assumes zero net cash flow (sum of buys ≈ sum of sells).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Rebalancing actions with buy/sell recommendations</returns>
    Task<RebalancingActionsDto> GetRebalancingActionsAsync(Guid userId);

    /// <summary>
    /// Generates smart rebalancing recommendations using proportional gap distribution.
    /// Identifies underweight assets and allocates investment amount proportionally based on gap scores.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Smart rebalancing request with investment amount and constraints</param>
    /// <returns>Smart rebalancing recommendations</returns>
    Task<SmartRebalancingResponseDto> GetSmartRecommendationsAsync(
        Guid userId,
        SmartRebalancingRequestDto request);
}

