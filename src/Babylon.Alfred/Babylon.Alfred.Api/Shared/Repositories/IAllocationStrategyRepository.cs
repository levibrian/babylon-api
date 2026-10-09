using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface IAllocationStrategyRepository
{
    Task<List<AllocationStrategy>> GetByUserIdAsync(Guid userId);
    Task ReplaceForUserAsync(Guid userId, IReadOnlyCollection<AllocationStrategy> strategies);
}
