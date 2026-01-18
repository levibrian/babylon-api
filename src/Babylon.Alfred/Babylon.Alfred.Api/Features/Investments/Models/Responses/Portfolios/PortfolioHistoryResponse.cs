namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

/// <summary>
/// Response containing historical portfolio snapshots.
/// </summary>
public class PortfolioHistoryResponse
{
    /// <summary>
    /// User ID for this portfolio history.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Start of the history range.
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// End of the history range.
    /// </summary>
    public DateTime? To { get; set; }

    /// <summary>
    /// Number of snapshots returned.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Historical portfolio snapshots ordered by timestamp ascending.
    /// </summary>
    public List<PortfolioSnapshotDto> Snapshots { get; set; } = [];

    /// <summary>
    /// Summary statistics for the period.
    /// </summary>
    public PortfolioHistorySummary? Summary { get; set; }
}

/// <summary>
/// Summary statistics for the portfolio history period.
/// </summary>
public class PortfolioHistorySummary
{
    /// <summary>
    /// Starting portfolio value (first snapshot).
    /// </summary>
    public decimal StartingValue { get; set; }

    /// <summary>
    /// Ending portfolio value (last snapshot).
    /// </summary>
    public decimal EndingValue { get; set; }

    /// <summary>
    /// Total change in value over the period.
    /// </summary>
    public decimal ValueChange { get; set; }

    /// <summary>
    /// Percentage change in value over the period.
    /// </summary>
    public decimal ValueChangePercentage { get; set; }

    /// <summary>
    /// Total unrealized PnL at the end of the period.
    /// </summary>
    public decimal NetPnL { get; set; }

    /// <summary>
    /// Total change in unrealized PnL over the period.
    /// </summary>
    public decimal PnLChange { get; set; }

    /// <summary>
    /// Percentage change in unrealized PnL over the period.
    /// </summary>
    public decimal PnLChangePercentage { get; set; }

    /// <summary>
    /// Highest portfolio value during the period.
    /// </summary>
    public decimal HighestValue { get; set; }

    /// <summary>
    /// Timestamp of highest portfolio value.
    /// </summary>
    public DateTime HighestValueTimestamp { get; set; }

    /// <summary>
    /// Lowest portfolio value during the period.
    /// </summary>
    public decimal LowestValue { get; set; }

    /// <summary>
    /// Timestamp of lowest portfolio value.
    /// </summary>
    public DateTime LowestValueTimestamp { get; set; }
}

