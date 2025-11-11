using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class AllocationStrategyRepository : IAllocationStrategyRepository
{
    private readonly BabylonDbContext context;

    public AllocationStrategyRepository(BabylonDbContext context)
    {
        this.context = context;
    }

    public async Task<Dictionary<string, decimal>> GetTargetAllocationsByUserIdAsync(Guid userId)
    {
        var strategies = await context.AllocationStrategies
            .Where(s => s.UserId == userId)
            .ToListAsync();

        return strategies.ToDictionary(s => s.Ticker, s => s.TargetPercentage);
    }

    public async Task SetAllocationStrategyAsync(Guid userId, List<AllocationStrategy> allocations)
    {
        // Get existing strategies for this user
        var existingStrategies = await context.AllocationStrategies
            .Where(s => s.UserId == userId)
            .ToListAsync();

        // Get tickers from new allocations
        var newTickers = allocations.Select(a => a.Ticker).ToHashSet();

        // Delete strategies that are no longer in the new list
        var toDelete = existingStrategies.Where(s => !newTickers.Contains(s.Ticker)).ToList();
        if (toDelete.Any())
        {
            context.AllocationStrategies.RemoveRange(toDelete);
        }

        // Update existing or add new
        foreach (var allocation in allocations)
        {
            var existing = existingStrategies.FirstOrDefault(s => s.Ticker == allocation.Ticker);
            if (existing != null)
            {
                existing.TargetPercentage = allocation.TargetPercentage;
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

    public async Task<List<string>> GetDistinctTickersAsync()
    {
        return await context.AllocationStrategies
            .Select(s => s.Ticker)
            .Distinct()
            .ToListAsync();
    }
}

