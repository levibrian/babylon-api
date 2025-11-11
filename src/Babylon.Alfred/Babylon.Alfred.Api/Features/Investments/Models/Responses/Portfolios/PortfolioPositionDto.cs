using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

public class PortfolioPositionDto
{
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal TotalInvested { get; set; }
    public decimal TotalShares { get; set; }
    public decimal AverageSharePrice { get; set; }
    public decimal TargetAllocation { get; set; }
    public List<PortfolioTransactionDto> Transactions { get; set; } = [];
}
