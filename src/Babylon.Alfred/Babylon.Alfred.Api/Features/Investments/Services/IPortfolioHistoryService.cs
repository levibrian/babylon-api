using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Service for retrieving historical portfolio snapshots.
/// </summary>
public interface IPortfolioHistoryService
{
    /// <summary>
    /// Gets historical portfolio snapshots for a user within an optional date/time range.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="from">Optional start timestamp (inclusive)</param>
    /// <param name="to">Optional end timestamp (inclusive)</param>
    /// <returns>Portfolio history response with snapshots and summary</returns>
    Task<PortfolioHistoryResponse> GetHistoryAsync(Guid userId, DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Gets the latest portfolio snapshot for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Latest snapshot or null if none exists</returns>
    Task<PortfolioSnapshotDto?> GetLatestSnapshotAsync(Guid userId);
}

