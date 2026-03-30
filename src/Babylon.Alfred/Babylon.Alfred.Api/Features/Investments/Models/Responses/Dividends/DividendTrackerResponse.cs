namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Dividends;

public class DividendTrackerResponse
{
    public List<DividendMonthDto> Paid { get; set; } = [];
    public List<DividendMonthDto> Projected { get; set; } = [];
}
