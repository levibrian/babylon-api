using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

public class PortfolioPositionDto
{
    public string Ticker { get; set; } = string.Empty;
    public string SecurityName { get; set; } = string.Empty;
    public SecurityType SecurityType { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal TotalShares { get; set; }
    public decimal AverageSharePrice { get; set; }

    // Security metadata
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public string? Geography { get; set; }
    public decimal? MarketCap { get; set; }

    // Allocation properties
    public decimal? CurrentAllocationPercentage { get; set; }
    public decimal? TargetAllocationPercentage { get; set; }
    public decimal? AllocationDeviation { get; set; } // current - target
    public decimal? RebalancingAmount { get; set; } // € amount (positive = buy, negative = sell)
    public RebalancingStatus RebalancingStatus { get; set; }
    public decimal? CurrentMarketValue { get; set; } // Current position value in €

    public List<PortfolioTransactionDto> Transactions { get; set; } = [];
}

public enum RebalancingStatus
{
    Balanced,      // Within ±1% of target
    Overweight,    // >1% above target
    Underweight    // >1% below target
}
