namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Dividends;

public class DividendMonthDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public string Label { get; set; } = string.Empty;
}
