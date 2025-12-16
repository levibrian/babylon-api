using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class PortfolioSnapshotRepository(BabylonDbContext context, ILogger<PortfolioSnapshotRepository> logger) 
    : IPortfolioSnapshotRepository
{
    public async Task UpsertSnapshotAsync(PortfolioSnapshot snapshot)
    {
        var existing = await context.PortfolioSnapshots
            .FirstOrDefaultAsync(ps => ps.UserId == snapshot.UserId && ps.SnapshotDate == snapshot.SnapshotDate);

        if (existing != null)
        {
            existing.TotalInvested = snapshot.TotalInvested;
            existing.TotalMarketValue = snapshot.TotalMarketValue;
            existing.UnrealizedPnL = snapshot.UnrealizedPnL;
            existing.UnrealizedPnLPercentage = snapshot.UnrealizedPnLPercentage;
            existing.CreatedAt = DateTime.UtcNow;
            context.PortfolioSnapshots.Update(existing);
            logger.LogDebug("Updated portfolio snapshot for user {UserId} on {Date}", snapshot.UserId, snapshot.SnapshotDate);
        }
        else
        {
            snapshot.Id = Guid.NewGuid();
            snapshot.CreatedAt = DateTime.UtcNow;
            await context.PortfolioSnapshots.AddAsync(snapshot);
            logger.LogDebug("Created portfolio snapshot for user {UserId} on {Date}", snapshot.UserId, snapshot.SnapshotDate);
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<PortfolioSnapshot>> GetSnapshotsByUserAsync(Guid userId, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        var query = context.PortfolioSnapshots
            .Where(ps => ps.UserId == userId);

        if (fromDate.HasValue)
        {
            query = query.Where(ps => ps.SnapshotDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(ps => ps.SnapshotDate <= toDate.Value);
        }

        return await query
            .OrderBy(ps => ps.SnapshotDate)
            .ToListAsync();
    }

    public async Task<PortfolioSnapshot?> GetLatestSnapshotAsync(Guid userId)
    {
        return await context.PortfolioSnapshots
            .Where(ps => ps.UserId == userId)
            .OrderByDescending(ps => ps.SnapshotDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Guid>> GetUserIdsWithPortfoliosAsync()
    {
        // Get distinct user IDs from transactions (users who have portfolio activity)
        return await context.Transactions
            .Where(t => t.UserId.HasValue)
            .Select(t => t.UserId!.Value)
            .Distinct()
            .ToListAsync();
    }
}

