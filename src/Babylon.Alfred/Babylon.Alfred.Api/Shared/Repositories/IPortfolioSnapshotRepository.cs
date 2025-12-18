using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface IPortfolioSnapshotRepository
{
    /// <summary>
    /// Adds a new portfolio snapshot.
    /// </summary>
    Task AddSnapshotAsync(PortfolioSnapshot snapshot);

    /// <summary>
    /// Gets portfolio snapshots for a user within a date/time range.
    /// </summary>
    Task<List<PortfolioSnapshot>> GetSnapshotsByUserAsync(Guid userId, DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Gets the latest snapshot for a user.
    /// </summary>
    Task<PortfolioSnapshot?> GetLatestSnapshotAsync(Guid userId);

    /// <summary>
    /// Gets all user IDs that have transactions (and thus portfolios to snapshot).
    /// </summary>
    Task<List<Guid>> GetUserIdsWithPortfoliosAsync();
}

