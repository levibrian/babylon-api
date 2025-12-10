using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for managing portfolio allocation strategies.
/// </summary>
[ApiController]
[Route("api/v1/portfolios/{userId:guid}/allocation")]
public class AllocationController(IAllocationStrategyService allocationStrategyService) : ControllerBase
{
    /// <summary>
    /// Sets or updates the allocation strategy for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">List of allocations with ticker and target percentage</param>
    /// <returns>No content on success</returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAllocationStrategy(
        Guid userId,
        [FromBody] SetAllocationStrategyRequest request)
    {
        try
        {
            await allocationStrategyService.SetAllocationStrategyAsync(userId, request.Allocations);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid allocation strategy",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets the current allocation strategy for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Allocation strategy with total allocated percentage</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AllocationStrategyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AllocationStrategyResponse>> GetAllocationStrategy(Guid userId)
    {
        var allocations = await allocationStrategyService.GetTargetAllocationsAsync(userId);
        var totalAllocated = await allocationStrategyService.GetTotalAllocatedPercentageAsync(userId);

        return Ok(new AllocationStrategyResponse
        {
            Allocations = allocations,
            TotalAllocated = totalAllocated
        });
    }
}

/// <summary>
/// Request to set allocation strategy.
/// </summary>
public class SetAllocationStrategyRequest
{
    /// <summary>
    /// List of allocation targets per ticker.
    /// </summary>
    public required List<AllocationStrategyDto> Allocations { get; init; }
}

