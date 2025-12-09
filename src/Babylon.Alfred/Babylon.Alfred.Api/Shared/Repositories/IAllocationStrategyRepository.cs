using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface IAllocationStrategyRepository
{
    Task<List<AllocationStrategy>> GetAllocationStrategiesByUserIdAsync(Guid userId);
    Task SetAllocationStrategyAsync(Guid userId, List<AllocationStrategy> allocations);
    Task<List<Guid>> GetDistinctSecurityIdsAsync();
}

