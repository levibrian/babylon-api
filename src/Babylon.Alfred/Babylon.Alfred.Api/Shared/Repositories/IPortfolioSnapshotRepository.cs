using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface IPortfolioSnapshotRepository
{
    /// <summary>
    /// Upserts a portfolio snapshot for a user on a specific date.
    /// If a snapshot already exists for that user/date, it will be updated.
    /// </summary>
    Task UpsertSnapshotAsync(PortfolioSnapshot snapshot);
    
    /// <summary>
    /// Gets portfolio snapshots for a user within a date range.
    /// </summary>
    Task<List<PortfolioSnapshot>> GetSnapshotsByUserAsync(Guid userId, DateOnly? fromDate = null, DateOnly? toDate = null);
    
    /// <summary>
    /// Gets the latest snapshot for a user.
    /// </summary>
    Task<PortfolioSnapshot?> GetLatestSnapshotAsync(Guid userId);
    
    /// <summary>
    /// Gets all user IDs that have transactions (and thus portfolios to snapshot).
    /// </summary>
    Task<List<Guid>> GetUserIdsWithPortfoliosAsync();
}

