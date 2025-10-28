namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

public class PortfolioPositionDto
{
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal TotalInvested { get; set; }
    public List<PortfolioTransactionDto> Transactions { get; set; } = [];
}
