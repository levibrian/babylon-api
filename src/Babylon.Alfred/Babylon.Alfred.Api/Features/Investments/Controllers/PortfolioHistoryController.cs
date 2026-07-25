using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Controllers;
using Babylon.Alfred.Api.Shared.Extensions;
using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[Authorize]
[Route("api/v1/portfolios/history")]
public class PortfolioHistoryController(IPortfolioHistoryService historyService) : BabylonControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PortfolioHistoryResponse>>> GetHistory(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userId = User.GetUserId();
        var history = await historyService.GetHistoryAsync(userId, from, to);
        return Success(history);
    }

    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<PortfolioSnapshotDto?>>> GetLatest()
    {
        var userId = User.GetUserId();
        var snapshot = await historyService.GetLatestSnapshotAsync(userId);
        return Success(snapshot);
    }
}

