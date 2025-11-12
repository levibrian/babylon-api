using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface IAllocationStrategyRepository
{
    Task<Dictionary<string, decimal>> GetTargetAllocationsByUserIdAsync(Guid userId); // Returns Dictionary<Ticker, TargetPercentage>
    Task SetAllocationStrategyAsync(Guid userId, List<AllocationStrategy> allocations);
    Task<List<Guid>> GetDistinctSecurityIdsAsync();
}

