using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Rebalancing;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for portfolio rebalancing actions and recommendations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/portfolios/rebalancing")]
public class RebalancingController(IRebalancingService rebalancingService) : ControllerBase
{
    /// <summary>
    /// Get rebalancing actions for the authenticated user's portfolio.
    /// </summary>
    /// <remarks>
    /// Calculates buy/sell actions needed to rebalance the portfolio to target allocations.
    /// Pure rebalancing assumes zero net cash flow (sum of buys ≈ sum of sells).
    /// </remarks>
    /// <returns>Rebalancing actions with buy/sell recommendations</returns>
    [HttpGet("actions")]
    [ProducesResponseType(typeof(RebalancingActionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RebalancingActionsDto>> GetRebalancingActions()
    {
        try
        {
            var userId = GetCurrentUserId();
            var actions = await rebalancingService.GetRebalancingActionsAsync(userId);
            return Ok(actions);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Portfolio not found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Calculate smart rebalancing recommendations for the authenticated user.
    /// </summary>
    /// <remarks>
    /// Uses proportional gap distribution algorithm to allocate investment amount
    /// across underweight positions based on their gap scores.
    /// </remarks>
    /// <param name="request">Smart rebalancing request with investment amount and constraints</param>
    /// <returns>Smart rebalancing recommendations</returns>
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(SmartRebalancingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SmartRebalancingResponseDto>> CalculateRecommendations(
        [FromBody] SmartRebalancingRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var recommendations = await rebalancingService.GetSmartRecommendationsAsync(userId, request);
            return Ok(recommendations);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Portfolio not found",
                Detail = ex.Message
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token.");
        }
        return userId;
    }
}

