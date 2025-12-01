using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;
using static Babylon.Alfred.Api.Constants.User;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/portfolios/insights")]
public class InsightsController(IPortfolioInsightsService portfolioInsightsService) : ControllerBase
{
    /// <summary>
    /// Gets top portfolio insights for a user (rebalancing recommendations, dividend watch, etc.).
    /// Returns a short list (max 3 by default) of high-impact, actionable insights.
    /// </summary>
    /// <param name="userId">User ID (optional, defaults to root user)</param>
    /// <param name="limit">Maximum number of insights to return (default: 3)</param>
    /// <returns>List of portfolio insights</returns>
    [HttpGet]
    public async Task<IActionResult> GetInsights(Guid? userId, [FromQuery] int limit = 3)
    {
        var effectiveUserId = userId ?? RootUserId;

        var insights = await portfolioInsightsService.GetTopInsightsAsync(effectiveUserId, limit);

        return Ok(new { insights });
    }
}

