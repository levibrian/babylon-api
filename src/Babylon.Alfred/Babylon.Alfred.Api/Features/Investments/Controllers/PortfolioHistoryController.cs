using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for portfolio history and performance tracking.
/// </summary>
[ApiController]
[Route("api/v1/portfolios/{userId:guid}/history")]
public class PortfolioHistoryController(IPortfolioHistoryService historyService) : ControllerBase
{
    /// <summary>
    /// Gets historical portfolio snapshots for a user.
    /// </summary>
    /// <remarks>
    /// Returns daily portfolio snapshots including total invested, market value, P&amp;L, and P&amp;L %.
    /// Optionally filter by date range. If no dates provided, returns all available history.
    /// 
    /// Example:
    /// - GET /api/v1/portfolios/{userId}/history
    /// - GET /api/v1/portfolios/{userId}/history?from=2024-01-01
    /// - GET /api/v1/portfolios/{userId}/history?from=2024-01-01&amp;to=2024-12-31
    /// </remarks>
    /// <param name="userId">User ID</param>
    /// <param name="from">Optional start date (inclusive, format: YYYY-MM-DD)</param>
    /// <param name="to">Optional end date (inclusive, format: YYYY-MM-DD)</param>
    /// <returns>Portfolio history with snapshots and summary statistics</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PortfolioHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PortfolioHistoryResponse>> GetHistory(
        Guid userId,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        // Validate date range
        if (from.HasValue && to.HasValue && from > to)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid date range",
                Detail = "'from' date must be before or equal to 'to' date"
            });
        }

        var history = await historyService.GetHistoryAsync(userId, from, to);
        return Ok(history);
    }

    /// <summary>
    /// Gets the latest portfolio snapshot for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Latest portfolio snapshot or 404 if none exists</returns>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(PortfolioSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PortfolioSnapshotDto>> GetLatest(Guid userId)
    {
        var snapshot = await historyService.GetLatestSnapshotAsync(userId);
        
        if (snapshot == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "No snapshot found",
                Detail = $"No portfolio snapshot found for user {userId}"
            });
        }

        return Ok(snapshot);
    }
}

