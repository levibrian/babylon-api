using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;
using static Babylon.Alfred.Api.Constants;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/portfolio/insights")]
public class InsightsController(IPortfolioInsightsService portfolioInsightsService) : ControllerBase
{
    /// <summary>
    /// Gets top portfolio insights for a user (rebalancing recommendations, performance milestones, etc.).
    /// </summary>
    /// <param name="userId">User ID (optional, defaults to root user)</param>
    /// <param name="limit">Maximum number of insights to return (default: 5)</param>
    /// <returns>List of portfolio insights</returns>
    [HttpGet]
    public async Task<IActionResult> GetInsights(Guid? userId, [FromQuery] int limit = 5)
    {
        var effectiveUserId = userId ?? Constants.User.RootUserId;

        var insights = await portfolioInsightsService.GetTopInsightsAsync(effectiveUserId, limit);

        return Ok(new { insights });
    }
}

