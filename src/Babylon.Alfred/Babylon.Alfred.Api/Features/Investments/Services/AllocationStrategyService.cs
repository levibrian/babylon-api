using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class AllocationStrategyService : IAllocationStrategyService
{
    private readonly IAllocationStrategyRepository _allocationStrategyRepository;

    public AllocationStrategyService(IAllocationStrategyRepository allocationStrategyRepository)
    {
        _allocationStrategyRepository = allocationStrategyRepository;
    }

    public async Task SetAllocationStrategyAsync(Guid userId, List<AllocationStrategyDto> allocations)
    {
        // Validate: sum cannot exceed 100%
        var totalPercentage = allocations.Sum(a => a.TargetPercentage);
        if (totalPercentage > 100)
        {
            throw new InvalidOperationException($"Total allocation percentage ({totalPercentage}%) cannot exceed 100%.");
        }

        // Convert DTOs to entities
        var allocationStrategies = allocations.Select(a => new AllocationStrategy
        {
            Ticker = a.Ticker,
            TargetPercentage = a.TargetPercentage
        }).ToList();

        await _allocationStrategyRepository.SetAllocationStrategyAsync(userId, allocationStrategies);
    }

    public async Task<Dictionary<string, decimal>> GetTargetAllocationsAsync(Guid userId)
    {
        return await _allocationStrategyRepository.GetTargetAllocationsByUserIdAsync(userId);
    }

    public async Task<decimal> GetTotalAllocatedPercentageAsync(Guid userId)
    {
        var allocations = await _allocationStrategyRepository.GetTargetAllocationsByUserIdAsync(userId);
        return allocations.Values.Sum();
    }
}

