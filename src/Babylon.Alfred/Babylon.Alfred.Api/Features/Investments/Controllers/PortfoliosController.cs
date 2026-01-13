using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Extensions;
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
        var userId = User.GetUserId();
        var portfolio = await portfolioService.GetPortfolio(userId);
        return Ok(portfolio);
    }
}
