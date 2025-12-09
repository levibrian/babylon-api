using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class AllocationStrategyRepository(BabylonDbContext context) : IAllocationStrategyRepository
{
    public async Task<List<AllocationStrategy>> GetAllocationStrategiesByUserIdAsync(Guid userId)
    {
        return await context.AllocationStrategies
            .Include(s => s.Security)
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }

    public async Task SetAllocationStrategyAsync(Guid userId, List<AllocationStrategy> allocations)
    {
        // Get existing strategies for this user
        var existingStrategies = await context.AllocationStrategies
            .Where(s => s.UserId == userId)
            .ToListAsync();

        // Get SecurityIds from new allocations
        var newSecurityIds = allocations.Select(a => a.SecurityId).ToHashSet();

        // Delete strategies that are no longer in the new list
        var toDelete = existingStrategies.Where(s => !newSecurityIds.Contains(s.SecurityId)).ToList();
        if (toDelete.Any())
        {
            context.AllocationStrategies.RemoveRange(toDelete);
        }

        // Update existing or add new
        foreach (var allocation in allocations)
        {
            var existing = existingStrategies.FirstOrDefault(s => s.SecurityId == allocation.SecurityId);
            if (existing != null)
            {
                existing.TargetPercentage = allocation.TargetPercentage;
                existing.IsEnabledForWeekly = allocation.IsEnabledForWeekly;
                existing.IsEnabledForBiWeekly = allocation.IsEnabledForBiWeekly;
                existing.IsEnabledForMonthly = allocation.IsEnabledForMonthly;
                existing.UpdatedAt = DateTime.UtcNow;
                context.AllocationStrategies.Update(existing);
            }
            else
            {
                allocation.UserId = userId;
                allocation.CreatedAt = DateTime.UtcNow;
                allocation.UpdatedAt = DateTime.UtcNow;
                await context.AllocationStrategies.AddAsync(allocation);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<Guid>> GetDistinctSecurityIdsAsync()
    {
        return await context.AllocationStrategies
            .Select(s => s.SecurityId)
            .Distinct()
            .ToListAsync();
    }
}

