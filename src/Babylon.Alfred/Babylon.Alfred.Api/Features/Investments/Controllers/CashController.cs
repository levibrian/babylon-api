using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Controllers;
using Babylon.Alfred.Api.Shared.Extensions;
using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[Authorize]
[Route("api/v1/cash")]
public class CashController(ICashBalanceService cashBalanceService) : BabylonControllerBase
{
    [HttpPut]
    public async Task<ActionResult<ApiResponse<object>>> UpdateBalance([FromBody] UpdateCashBalanceRequest request)
    {
        await cashBalanceService.UpdateManualBalanceAsync(User.GetUserId(), request.Amount);
        return Success();
    }
}
