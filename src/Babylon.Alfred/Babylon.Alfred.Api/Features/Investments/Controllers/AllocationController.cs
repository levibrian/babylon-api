using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for managing portfolio allocation strategies.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/portfolios/allocation")]
public class AllocationController(IAllocationStrategyService allocationStrategyService) : ControllerBase
{
    /// <summary>
    /// Sets or updates the allocation strategy for the authenticated user.
    /// </summary>
    /// <param name="request">List of allocations with ticker and target percentage</param>
    /// <returns>No content on success</returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAllocationStrategy(
        [FromBody] SetAllocationStrategyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
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
    /// Gets the current allocation strategy for the authenticated user.
    /// </summary>
    /// <returns>Allocation strategy with total allocated percentage</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AllocationStrategyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AllocationStrategyResponse>> GetAllocationStrategy()
    {
        var userId = GetCurrentUserId();
        var allocations = await allocationStrategyService.GetTargetAllocationsAsync(userId);
        var totalAllocated = await allocationStrategyService.GetTotalAllocatedPercentageAsync(userId);

        return Ok(new AllocationStrategyResponse
        {
            Allocations = allocations,
            TotalAllocated = totalAllocated
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token.");
        }
        return userId;
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

