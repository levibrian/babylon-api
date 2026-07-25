using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Controllers;
using Babylon.Alfred.Api.Shared.Extensions;
using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[Authorize]
[Route("api/v1/portfolios")]
public class PortfoliosController(IPortfolioService portfolioService) : BabylonControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PortfolioResponse>>> Get()
    {
        var userId = User.GetUserId();
        var portfolio = await portfolioService.GetPortfolio(userId);
        return Success(portfolio);
    }
}
