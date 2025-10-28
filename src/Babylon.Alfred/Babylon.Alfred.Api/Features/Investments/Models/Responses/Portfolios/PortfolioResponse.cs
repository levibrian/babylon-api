namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

public class PortfolioResponse
{
    public List<PortfolioPositionDto> Positions { get; set; } = [];
    public decimal TotalInvested { get; set; }
}
