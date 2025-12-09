using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models.Responses;

public class CompanyDto(string ticker, string securityName, SecurityType securityType, string? currency = null, string? exchange = null)
{
    public string Ticker { get; set; } = ticker;
    public string SecurityName { get; set; } = securityName;
    public SecurityType SecurityType { get; set; } = securityType;
    public string? Currency { get; set; } = currency;
    public string? Exchange { get; set; } = exchange;
}
