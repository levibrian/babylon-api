using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for portfolio insights and recommendations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/portfolios/insights")]
public class InsightsController(IPortfolioInsightsService portfolioInsightsService) : ControllerBase
{
    /// <summary>
    /// Gets top portfolio insights for the authenticated user.
    /// </summary>
    /// <remarks>
    /// Returns a short list of high-impact, actionable insights including:
    /// rebalancing recommendations, dividend opportunities, risk alerts, etc.
    /// </remarks>
    /// <param name="limit">Maximum number of insights to return (default: 3, max: 10)</param>
    /// <returns>List of portfolio insights</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<PortfolioInsightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PortfolioInsightDto>>> GetInsights(
        [FromQuery] int limit = 3)
    {
        if (limit is < 1 or > 10)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid limit",
                Detail = "Limit must be between 1 and 10"
            });
        }

        var userId = GetCurrentUserId();
        var insights = await portfolioInsightsService.GetTopInsightsAsync(userId, limit);
        return Ok(insights);
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

