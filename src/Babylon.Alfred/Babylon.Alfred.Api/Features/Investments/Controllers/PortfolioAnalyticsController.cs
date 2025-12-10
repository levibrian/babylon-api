using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for portfolio analytics including diversification and risk metrics.
/// </summary>
[ApiController]
[Route("api/v1/portfolios/{userId:guid}/analytics")]
public class PortfolioAnalyticsController(IPortfolioAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>
    /// Get diversification metrics for a user's portfolio.
    /// Calculates HHI, Effective Number of Bets, Diversification Score, and concentration metrics.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Diversification metrics</returns>
    [HttpGet("diversification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiversification(Guid userId)
    {
        try
        {
            var metrics = await analyticsService.GetDiversificationMetricsAsync(userId);
            return Ok(metrics);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Get risk metrics for a user's portfolio.
    /// Calculates volatility, beta, and Sharpe ratio.
    /// Note: Currently not implemented - requires historical price data.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="period">Time period for analysis (1Y, 3M, 6M)</param>
    /// <returns>Risk metrics</returns>
    [HttpGet("risk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> GetRisk(Guid userId, [FromQuery] string period = "1Y")
    {
        try
        {
            var metrics = await analyticsService.GetRiskMetricsAsync(userId, period);
            return Ok(metrics);
        }
        catch (NotImplementedException ex)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
