namespace Babylon.Alfred.Api.Features.Investments.Models.Requests;

public class AllocationStrategyDto
{
    public required string Ticker { get; set; }
    public decimal TargetPercentage { get; set; }
}

