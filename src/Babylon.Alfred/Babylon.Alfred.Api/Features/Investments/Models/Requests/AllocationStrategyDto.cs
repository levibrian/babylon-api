namespace Babylon.Alfred.Api.Features.Investments.Models.Requests;

public class AllocationStrategyDto
{
    public required string Ticker { get; set; }
    public decimal TargetPercentage { get; set; }
    public bool IsEnabledForWeekly { get; set; }
    public bool IsEnabledForBiWeekly { get; set; }
    public bool IsEnabledForMonthly { get; set; }
}

