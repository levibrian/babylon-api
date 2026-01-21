using Babylon.Alfred.Api.Features.Investments.Models.Responses.Rebalancing;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface ITimedRebalancingActionsService
{
    /// <summary>
    /// Gets timed rebalancing actions with optional AI optimization.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="investmentAmount">Optional additional investment amount</param>
    /// <param name="maxActions">Maximum number of actions to return</param>
    /// <param name="useAi">Whether to use AI optimization (requires Gemini to be enabled)</param>
    Task<TimedRebalancingActionsResponseDto> GetTimedActionsAsync(
        Guid userId,
        decimal? investmentAmount,
        int? maxActions,
        bool useAi = false);
}
