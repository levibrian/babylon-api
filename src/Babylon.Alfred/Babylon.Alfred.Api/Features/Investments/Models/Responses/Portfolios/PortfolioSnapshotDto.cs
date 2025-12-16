namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

/// <summary>
/// Represents a historical portfolio snapshot for a specific date.
/// </summary>
public class PortfolioSnapshotDto
{
    /// <summary>
    /// The date of the snapshot.
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
}

