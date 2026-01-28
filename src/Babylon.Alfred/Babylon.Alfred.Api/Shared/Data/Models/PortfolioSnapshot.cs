namespace Babylon.Alfred.Api.Shared.Data.Models;

/// <summary>
/// Stores hourly portfolio performance snapshots for historical tracking.
/// Captures portfolio value throughout the trading day for intraday charts.
/// </summary>
public class PortfolioSnapshot
{
    public Guid Id { get; set; }

    /// <summary>
    /// The user who owns this portfolio snapshot.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The timestamp of the snapshot (includes date and time for hourly snapshots).
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
    /// Unrealized profit/loss (TotalMarketValue - TotalInvested).
    /// </summary>
    public decimal UnrealizedPnL { get; set; }

    /// <summary>
    /// Unrealized profit/loss as a percentage of TotalInvested.
    /// </summary>
    public decimal UnrealizedPnLPercentage { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}

