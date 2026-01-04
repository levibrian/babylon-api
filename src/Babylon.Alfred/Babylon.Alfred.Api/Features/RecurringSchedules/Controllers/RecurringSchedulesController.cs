using Babylon.Alfred.Api.Features.RecurringSchedules.Models.Requests;
using Babylon.Alfred.Api.Features.RecurringSchedules.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.RecurringSchedules.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/recurring-schedules")]
public class RecurringSchedulesController(IRecurringScheduleService recurringScheduleService) : ControllerBase
{
    /// <summary>
    /// Creates or updates a recurring investment schedule.
    /// If a schedule already exists for the user and security, it will be reactivated and updated.
    /// </summary>
    /// <param name="request">Recurring schedule creation request</param>
    /// <returns>Created or updated recurring schedule</returns>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateRecurringSchedule(CreateRecurringScheduleRequest request)
    {
        var userId = GetCurrentUserId();
        var schedule = await recurringScheduleService.CreateOrUpdateAsync(userId, request);
        return Ok(schedule);
    }

    /// <summary>
    /// Gets all active recurring schedules for the authenticated user.
    /// Results are grouped by Platform and sorted by Ticker.
    /// </summary>
    /// <returns>List of active recurring schedules</returns>
    [HttpGet]
    public async Task<IActionResult> GetRecurringSchedules()
    {
        var userId = GetCurrentUserId();
        var schedules = await recurringScheduleService.GetActiveByUserIdAsync(userId);
        return Ok(schedules);
    }

    /// <summary>
    /// Soft deletes a recurring schedule by setting IsActive to false.
    /// </summary>
    /// <param name="id">Recurring schedule ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecurringSchedule(Guid id)
    {
        await recurringScheduleService.DeleteAsync(id);
        return Ok(new { message = "Recurring schedule deleted successfully" });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) 
                          ?? User.Claims.FirstOrDefault(c => c.Type == "sub"); // Fallback

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        // This should theoretically not happen with [Authorize] unless the token is missing 'sub' claim
        throw new UnauthorizedAccessException("User ID not found in token");
    }
}

