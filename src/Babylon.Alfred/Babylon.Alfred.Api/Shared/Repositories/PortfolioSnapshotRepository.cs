using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class PortfolioSnapshotRepository(BabylonDbContext context, ILogger<PortfolioSnapshotRepository> logger)
    : IPortfolioSnapshotRepository
{
    public async Task AddSnapshotAsync(PortfolioSnapshot snapshot)
    {
        snapshot.Id = Guid.NewGuid();
        snapshot.Timestamp = DateTime.UtcNow;
        await context.PortfolioSnapshots.AddAsync(snapshot);
        await context.SaveChangesAsync();

        logger.LogDebug("Created portfolio snapshot for user {UserId} at {Timestamp}", snapshot.UserId, snapshot.Timestamp);
    }

    public async Task<List<PortfolioSnapshot>> GetSnapshotsByUserAsync(Guid userId, DateTime? from = null, DateTime? to = null)
    {
        var query = context.PortfolioSnapshots
            .Where(ps => ps.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(ps => ps.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(ps => ps.Timestamp <= to.Value);
        }

        return await query
            .OrderBy(ps => ps.Timestamp)
            .ToListAsync();
    }

    public async Task<PortfolioSnapshot?> GetLatestSnapshotAsync(Guid userId)
    {
        return await context.PortfolioSnapshots
            .Where(ps => ps.UserId == userId)
            .OrderByDescending(ps => ps.Timestamp)
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

