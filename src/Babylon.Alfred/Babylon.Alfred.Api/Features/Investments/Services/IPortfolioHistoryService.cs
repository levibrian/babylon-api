using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Service for retrieving historical portfolio snapshots.
/// </summary>
public interface IPortfolioHistoryService
{
    /// <summary>
    /// Gets historical portfolio snapshots for a user within an optional date range.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="fromDate">Optional start date (inclusive)</param>
    /// <param name="toDate">Optional end date (inclusive)</param>
    /// <returns>Portfolio history response with snapshots and summary</returns>
    Task<PortfolioHistoryResponse> GetHistoryAsync(Guid userId, DateOnly? fromDate = null, DateOnly? toDate = null);
    
    /// <summary>
    /// Gets the latest portfolio snapshot for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Latest snapshot or null if none exists</returns>
    Task<PortfolioSnapshotDto?> GetLatestSnapshotAsync(Guid userId);
}

