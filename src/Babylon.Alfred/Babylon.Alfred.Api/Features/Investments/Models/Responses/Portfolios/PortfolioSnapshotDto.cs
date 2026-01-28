namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

/// <summary>
/// Represents a historical portfolio snapshot at a specific point in time.
/// </summary>
public class PortfolioSnapshotDto
{
    /// <summary>
    /// The timestamp of the snapshot (includes date and time for hourly granularity).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Total cost basis (sum of all purchases) at snapshot time.
    /// </summary>
    public decimal TotalInvested { get; set; }

    /// <summary>
    /// Cash balance at snapshot time.
    /// </summary>
    public decimal CashBalance { get; set; }

    /// <summary>
    /// Total market value of portfolio at snapshot time.
    /// </summary>
    public decimal TotalMarketValue { get; set; }

    /// <summary>
    /// Unrealized profit/loss (Total Assets Market Value - Total Invested). Excludes Cash Balance.
    /// </summary>
    public decimal UnrealizedPnL { get; set; }

    /// <summary>
    /// Unrealized profit/loss as a percentage of TotalInvested.
    /// </summary>
    public decimal UnrealizedPnLPercentage { get; set; }
}

