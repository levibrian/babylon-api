using Babylon.Alfred.Api.Shared;
using Babylon.Alfred.Api.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UserController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // For now, we assume root user. In a real app, we'd get ID from claims.
        var userId = Constants.User.RootUserId;
        var user = await userRepository.GetUserAsync(userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.MonthlyInvestmentAmount
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
    {
        var userId = Constants.User.RootUserId;
        var user = await userRepository.GetUserAsync(userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.MonthlyInvestmentAmount = request.MonthlyInvestmentAmount;
        await userRepository.UpdateUserAsync(user);

        return Ok(new
        {
            message = "User updated successfully",
            user.MonthlyInvestmentAmount
        });
    }
}

public class UpdateUserRequest
{
    public decimal MonthlyInvestmentAmount { get; set; }
}
