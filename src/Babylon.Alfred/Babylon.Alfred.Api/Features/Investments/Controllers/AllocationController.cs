using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;
using static Babylon.Alfred.Api.Constants;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/portfolios/allocation")]
public class AllocationController(IAllocationStrategyService allocationStrategyService) : ControllerBase
{
    /// <summary>
    /// Sets or updates the allocation strategy for a user.
    /// </summary>
    /// <param name="userId">User ID (optional, defaults to root user)</param>
    /// <param name="request">List of allocations with ticker and target percentage</param>
    /// <returns>Success message</returns>
    [HttpPut]
    public async Task<IActionResult> SetAllocationStrategy(
        Guid? userId,
        [FromBody] SetAllocationStrategyRequest request)
    {
        var effectiveUserId = userId ?? Constants.User.RootUserId;

        await allocationStrategyService.SetAllocationStrategyAsync(effectiveUserId, request.Allocations);

        return Ok(new { message = "Allocation strategy updated successfully" });
    }

    /// <summary>
    /// Gets the current allocation strategy for a user.
    /// </summary>
    /// <param name="userId">User ID (optional, defaults to root user)</param>
    /// <returns>Allocation strategy with total allocated percentage</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllocationStrategy(Guid? userId)
    {
        var effectiveUserId = userId ?? Constants.User.RootUserId;

        var allocations = await allocationStrategyService.GetTargetAllocationsAsync(effectiveUserId);
        var totalAllocated = await allocationStrategyService.GetTotalAllocatedPercentageAsync(effectiveUserId);

        var response = new AllocationStrategyResponse
        {
            Allocations = allocations.Select(kvp => new AllocationStrategyDto
            {
                Ticker = kvp.Key,
                TargetPercentage = kvp.Value
            }).ToList(),
            TotalAllocated = totalAllocated
        };

        return Ok(response);
    }
}

public class SetAllocationStrategyRequest
{
    public required List<AllocationStrategyDto> Allocations { get; set; }
}

