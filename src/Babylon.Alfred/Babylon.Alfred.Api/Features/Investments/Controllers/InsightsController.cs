using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for portfolio insights and recommendations.
/// </summary>
[ApiController]
[Route("api/v1/portfolios/{userId:guid}/insights")]
public class InsightsController(IPortfolioInsightsService portfolioInsightsService) : ControllerBase
{
    /// <summary>
    /// Gets top portfolio insights for a user.
    /// </summary>
    /// <remarks>
    /// Returns a short list of high-impact, actionable insights including:
    /// rebalancing recommendations, dividend opportunities, risk alerts, etc.
    /// </remarks>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Maximum number of insights to return (default: 3, max: 10)</param>
    /// <returns>List of portfolio insights</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<PortfolioInsightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PortfolioInsightDto>>> GetInsights(
        Guid userId,
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

        var insights = await portfolioInsightsService.GetTopInsightsAsync(userId, limit);
        return Ok(insights);
    }
}

