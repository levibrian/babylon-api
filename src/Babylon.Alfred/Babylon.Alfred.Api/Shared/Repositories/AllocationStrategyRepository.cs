using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class AllocationStrategyRepository(BabylonDbContext context) : IAllocationStrategyRepository
{
    public async Task<List<AllocationStrategy>> GetByUserIdAsync(Guid userId)
    {
        return await context.AllocationStrategies
            .AsNoTracking()
            .Include(a => a.Security)
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task ReplaceForUserAsync(Guid userId, IReadOnlyCollection<AllocationStrategy> strategies)
    {
        var existing = await context.AllocationStrategies
            .Where(a => a.UserId == userId)
            .ToListAsync();
        var incomingBySecurityId = strategies.ToDictionary(s => s.SecurityId);

        context.AllocationStrategies.RemoveRange(existing.Where(e => !incomingBySecurityId.ContainsKey(e.SecurityId)));

        foreach (var current in existing.Where(e => incomingBySecurityId.ContainsKey(e.SecurityId)))
        {
            var incoming = incomingBySecurityId[current.SecurityId];
            current.TargetPercentage = incoming.TargetPercentage;
            current.UpdatedAt = incoming.UpdatedAt;
        }

        var existingSecurityIds = existing.Select(e => e.SecurityId).ToHashSet();
        foreach (var incoming in strategies.Where(s => !existingSecurityIds.Contains(s.SecurityId)))
        {
            incoming.UserId = userId;
            await context.AllocationStrategies.AddAsync(incoming);
        }

        await context.SaveChangesAsync();
    }
}
