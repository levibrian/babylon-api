using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for portfolio management.
/// </summary>
[ApiController]
[Route("api/v1/portfolios")]
public class PortfoliosController(IPortfolioService portfolioService) : ControllerBase
{
    /// <summary>
    /// Gets the portfolio for a user including all positions, allocations, and market values.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Portfolio with positions and allocations</returns>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(PortfolioResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PortfolioResponse>> Get(Guid userId)
    {
        var portfolio = await portfolioService.GetPortfolio(userId);
        return Ok(portfolio);
    }
}
