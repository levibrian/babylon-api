namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

public class PortfolioResponse
{
    public List<PortfolioPositionDto> Positions { get; set; } = [];
    public decimal CashAmount { get; set; }

    /// <summary>
    /// Total cost basis (sum of all purchases).
    /// </summary>
    public decimal TotalInvested { get; set; }

    /// <summary>
    /// Total current market value (sum of all position values).
    /// </summary>
    public decimal? TotalMarketValue { get; set; }

    /// <summary>
    /// Total unrealized P&L (TotalMarketValue - TotalInvested).
    /// </summary>
    public decimal? TotalUnrealizedPnL { get; set; }

    /// <summary>
    /// Total unrealized P&L percentage.
    /// </summary>
    public decimal? TotalUnrealizedPnLPercentage { get; set; }
}
