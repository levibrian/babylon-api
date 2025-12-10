using Babylon.Alfred.Api.Features.Investments.Models.Responses.Analytics;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for portfolio analytics including diversification and risk metrics.
/// </summary>
[ApiController]
[Route("api/v1/portfolios/{userId:guid}")]
public class PortfolioAnalyticsController(IPortfolioAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>
    /// Get diversification metrics for a user's portfolio.
    /// </summary>
    /// <remarks>
    /// Calculates HHI, Effective Number of Bets, Diversification Score, and concentration metrics.
    /// </remarks>
    /// <param name="userId">User ID</param>
    /// <returns>Diversification metrics</returns>
    [HttpGet("diversification")]
    [ProducesResponseType(typeof(DiversificationMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiversificationMetricsDto>> GetDiversification(Guid userId)
    {
        try
        {
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
    /// Get risk metrics for a user's portfolio.
    /// </summary>
    /// <remarks>
    /// Calculates volatility, beta, and Sharpe ratio using historical price data.
    /// </remarks>
    /// <param name="userId">User ID</param>
    /// <param name="period">Time period for analysis: 1Y, 6M, or 3M</param>
    /// <returns>Risk metrics</returns>
    [HttpGet("risk")]
    [ProducesResponseType(typeof(RiskMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RiskMetricsDto>> GetRisk(Guid userId, [FromQuery] string period = "1Y")
    {
        try
        {
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
