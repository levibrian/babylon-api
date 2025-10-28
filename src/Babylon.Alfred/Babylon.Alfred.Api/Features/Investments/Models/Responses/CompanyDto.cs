namespace Babylon.Alfred.Api.Features.Investments.Models.Responses;

public class CompanyDto(string ticker, string companyName)
{
    public string Ticker { get; set; } = ticker;
    public string CompanyName { get; set; } = companyName;
}
