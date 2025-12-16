namespace Babylon.Alfred.Api.Shared.Data.Models;

/// <summary>
/// Stores daily portfolio performance snapshots for historical tracking.
/// One snapshot per user per day to track portfolio growth over time.
/// </summary>
public class PortfolioSnapshot
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The user who owns this portfolio snapshot.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The date of the snapshot (date only, no time component).
    /// Combined with UserId forms a unique constraint.
    /// </summary>
    public DateOnly SnapshotDate { get; set; }
    
    /// <summary>
    /// Total cost basis (sum of all purchases) at snapshot time.
    /// </summary>
    public decimal TotalInvested { get; set; }
    
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
    
    /// <summary>
    /// When this snapshot was created/updated.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
}

