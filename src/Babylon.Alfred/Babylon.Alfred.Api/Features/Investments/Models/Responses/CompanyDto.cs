using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models.Responses;

public class CompanyDto(string ticker, string securityName, SecurityType securityType)
{
    public string Ticker { get; set; } = ticker;
    public string SecurityName { get; set; } = securityName;
    public SecurityType SecurityType { get; set; } = securityType;
}
