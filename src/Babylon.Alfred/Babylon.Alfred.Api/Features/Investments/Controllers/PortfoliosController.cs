using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for portfolio management.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/portfolios")]
public class PortfoliosController(IPortfolioService portfolioService) : ControllerBase
{
    /// <summary>
    /// Gets the portfolio for the authenticated user including all positions, allocations, and market values.
    /// </summary>
    /// <returns>Portfolio with positions and allocations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PortfolioResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PortfolioResponse>> Get()
    {
        var userId = GetCurrentUserId();
        var portfolio = await portfolioService.GetPortfolio(userId);
        return Ok(portfolio);
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
