using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class AllocationStrategyService(
    IAllocationStrategyRepository allocationStrategyRepository,
    ISecurityRepository securityRepository)
    : IAllocationStrategyService
{
    public async Task SetAllocationStrategyAsync(Guid userId, List<AllocationStrategyDto> allocations)
    {
        // Validate: sum cannot exceed 100%
        var totalPercentage = allocations.Sum(a => a.TargetPercentage);
        if (totalPercentage > 100)
        {
            throw new InvalidOperationException($"Total allocation percentage ({totalPercentage}%) cannot exceed 100%.");
        }

        // Get all unique tickers and fetch securities
        var tickers = allocations.Select(a => a.Ticker).Distinct().ToList();
        var securities = await securityRepository.GetByTickersAsync(tickers);

        // Validate all tickers exist
        var missingTickers = tickers.Where(t => !securities.ContainsKey(t)).ToList();
        if (missingTickers.Any())
        {
            throw new InvalidOperationException($"Securities not found for tickers: {string.Join(", ", missingTickers)}");
        }

        // Convert DTOs to entities with SecurityId
        var allocationStrategies = allocations.Select(a => new AllocationStrategy
        {
            SecurityId = securities[a.Ticker].Id,
            TargetPercentage = a.TargetPercentage
        }).ToList();

        await allocationStrategyRepository.SetAllocationStrategyAsync(userId, allocationStrategies);
    }

    public async Task<Dictionary<string, decimal>> GetTargetAllocationsAsync(Guid userId)
    {
        return await allocationStrategyRepository.GetTargetAllocationsByUserIdAsync(userId);
    }

    public async Task<decimal> GetTotalAllocatedPercentageAsync(Guid userId)
    {
        var allocations = await allocationStrategyRepository.GetTargetAllocationsByUserIdAsync(userId);
        return allocations.Values.Sum();
    }
}

