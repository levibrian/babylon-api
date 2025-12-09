using Babylon.Alfred.Api.Features.Investments.Models.Requests;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface IAllocationStrategyService
{
    Task SetAllocationStrategyAsync(Guid userId, List<AllocationStrategyDto> allocations);
    Task<List<AllocationStrategyDto>> GetTargetAllocationsAsync(Guid userId);
    Task<decimal> GetTotalAllocatedPercentageAsync(Guid userId);
}

