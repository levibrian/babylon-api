using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/cash")]
public class CashController(ICashBalanceService cashBalanceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CashBalanceResponse>> GetBalance()
    {
        var userId = User.GetUserId();
        var balance = await cashBalanceService.GetBalanceAsync(userId);
        return Ok(new CashBalanceResponse {Balance = balance});
    }

    [HttpPut]
    public async Task<IActionResult> UpdateBalance([FromBody] UpdateCashBalanceRequest request)
    {
        try
        {
            var userId = User.GetUserId();
            await cashBalanceService.UpdateManualBalanceAsync(userId, request.Amount);
            return Ok(new { message = "Cash balance updated successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
