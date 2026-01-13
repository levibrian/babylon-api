using Babylon.Alfred.Api.Features.Investments.Models.Responses.Analytics;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for portfolio analytics including diversification and risk metrics.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/portfolios/analytics")]
public class PortfolioAnalyticsController(IPortfolioAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>
    /// Get diversification metrics for the authenticated user's portfolio.
    /// </summary>
    /// <remarks>
    /// Calculates HHI, Effective Number of Bets, Diversification Score, and concentration metrics.
    /// </remarks>
    /// <returns>Diversification metrics</returns>
    [HttpGet("diversification")]
    [ProducesResponseType(typeof(DiversificationMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiversificationMetricsDto>> GetDiversification()
    {
        try
        {
            var userId = User.GetUserId();
            var metrics = await analyticsService.GetDiversificationMetricsAsync(userId);
            return Ok(metrics);
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
    /// Get risk metrics for the authenticated user's portfolio.
    /// </summary>
    /// <remarks>
    /// Calculates volatility, beta, and Sharpe ratio using historical price data.
    /// </remarks>
    /// <param name="period">Time period for analysis: 1Y, 6M, or 3M</param>
    /// <returns>Risk metrics</returns>
    [HttpGet("risk")]
    [ProducesResponseType(typeof(RiskMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RiskMetricsDto>> GetRisk([FromQuery] string period = "1Y")
    {
        try
        {
            var userId = User.GetUserId();
            var metrics = await analyticsService.GetRiskMetricsAsync(userId, period);
            return Ok(metrics);
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
                Title = "Insufficient data",
                Detail = ex.Message
            });
        }
    }
}
