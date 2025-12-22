using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Service for retrieving historical portfolio snapshots.
/// </summary>
public class PortfolioHistoryService(IPortfolioSnapshotRepository snapshotRepository) : IPortfolioHistoryService
{
    public async Task<PortfolioHistoryResponse> GetHistoryAsync(Guid userId, DateTime? from = null, DateTime? to = null)
    {
        var snapshots = await snapshotRepository.GetSnapshotsByUserAsync(userId, from, to);

        var snapshotDtos = snapshots.Select(MapToDto).ToList();

        return new PortfolioHistoryResponse
        {
            UserId = userId,
            From = from,
            To = to,
            Count = snapshotDtos.Count,
            Snapshots = snapshotDtos,
            Summary = CalculateSummary(snapshotDtos)
        };
    }

    public async Task<PortfolioSnapshotDto?> GetLatestSnapshotAsync(Guid userId)
    {
        var snapshot = await snapshotRepository.GetLatestSnapshotAsync(userId);
        return snapshot != null ? MapToDto(snapshot) : null;
    }

    private static PortfolioSnapshotDto MapToDto(PortfolioSnapshot snapshot)
    {
        return new PortfolioSnapshotDto
        {
            Timestamp = snapshot.Timestamp,
            TotalInvested = snapshot.TotalInvested,
            TotalMarketValue = snapshot.TotalMarketValue,
            UnrealizedPnL = snapshot.UnrealizedPnL,
            UnrealizedPnLPercentage = snapshot.UnrealizedPnLPercentage
        };
    }

    private static PortfolioHistorySummary? CalculateSummary(List<PortfolioSnapshotDto> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return null;
        }

        var first = snapshots[0];
        var last = snapshots[^1];

        var highestSnapshot = snapshots.MaxBy(s => s.TotalMarketValue)!;
        var lowestSnapshot = snapshots.MinBy(s => s.TotalMarketValue)!;

        var valueChange = last.TotalMarketValue - first.TotalMarketValue;
        var valueChangePercentage = first.TotalMarketValue > 0
            ? (valueChange / first.TotalMarketValue) * 100
            : 0;

        return new PortfolioHistorySummary
        {
            StartingValue = first.TotalMarketValue,
            EndingValue = last.TotalMarketValue,
            ValueChange = Math.Round(valueChange, 2),
            ValueChangePercentage = Math.Round(valueChangePercentage, 2),
            HighestValue = highestSnapshot.TotalMarketValue,
            HighestValueTimestamp = highestSnapshot.Timestamp,
            LowestValue = lowestSnapshot.TotalMarketValue,
            LowestValueTimestamp = lowestSnapshot.Timestamp
        };
    }
}

