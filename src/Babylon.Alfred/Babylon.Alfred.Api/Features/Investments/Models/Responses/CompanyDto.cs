namespace Babylon.Alfred.Api.Features.Investments.Models.Responses;

public class CompanyDto(string ticker, string securityName)
{
    public string Ticker { get; set; } = ticker;
    public string SecurityName { get; set; } = securityName;
}
