using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/portfolios")]
public class PortfoliosController(IPortfolioService portfolioService) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(string userId)
    {
        var portfolio = await portfolioService.GetPortfolio();

        return Ok();
    }
}
