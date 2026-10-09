namespace Babylon.Alfred.Api.Features.Investments.Models.Requests;

public class UpdateAllocationTargetsRequest
{
    public List<AllocationTargetRequest> Targets { get; set; } = new();
}

public class AllocationTargetRequest
{
    public string Ticker { get; set; } = string.Empty;
    public decimal TargetPercentage { get; set; }
}
