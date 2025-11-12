using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class AllocationStrategyService : IAllocationStrategyService
{
    private readonly IAllocationStrategyRepository _allocationStrategyRepository;
    private readonly ICompanyRepository _companyRepository;

    public AllocationStrategyService(
        IAllocationStrategyRepository allocationStrategyRepository,
        ICompanyRepository companyRepository)
    {
        _allocationStrategyRepository = allocationStrategyRepository;
        _companyRepository = companyRepository;
    }

    public async Task SetAllocationStrategyAsync(Guid userId, List<AllocationStrategyDto> allocations)
    {
        // Validate: sum cannot exceed 100%
        var totalPercentage = allocations.Sum(a => a.TargetPercentage);
        if (totalPercentage > 100)
        {
            throw new InvalidOperationException($"Total allocation percentage ({totalPercentage}%) cannot exceed 100%.");
        }

        // Get all unique tickers and fetch companies
        var tickers = allocations.Select(a => a.Ticker).Distinct().ToList();
        var companies = await _companyRepository.GetByTickersAsync(tickers);

        // Validate all tickers exist
        var missingTickers = tickers.Where(t => !companies.ContainsKey(t)).ToList();
        if (missingTickers.Any())
        {
            throw new InvalidOperationException($"Companies not found for tickers: {string.Join(", ", missingTickers)}");
        }

        // Convert DTOs to entities with CompanyId
        var allocationStrategies = allocations.Select(a => new AllocationStrategy
        {
            CompanyId = companies[a.Ticker].Id,
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

