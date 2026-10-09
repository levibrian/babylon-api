namespace Babylon.Alfred.Api.Features.Investments.Models.Responses;

public class AllocationTargetDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal TargetPercentage { get; set; }
}
