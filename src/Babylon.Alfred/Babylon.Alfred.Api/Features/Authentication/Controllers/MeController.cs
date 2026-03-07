using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Features.Authentication.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Authentication.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController(IPasswordService passwordService) : ControllerBase
{
    [HttpPost("password")]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        var userId = User.GetUserId();
        await passwordService.UpdatePassword(userId, request.CurrentPassword, request.Password);
        return Ok(new ApiResponse<object> { Success = true, Data = new { } });
    }
}
