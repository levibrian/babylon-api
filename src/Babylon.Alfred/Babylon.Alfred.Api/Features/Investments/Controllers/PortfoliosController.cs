using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/portfolios")]
public class PortfoliosController(IPortfolioService portfolioService) : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(Guid? userId)
    {
        var portfolio = await portfolioService.GetPortfolio(userId);

        return Ok(portfolio);
    }
}
