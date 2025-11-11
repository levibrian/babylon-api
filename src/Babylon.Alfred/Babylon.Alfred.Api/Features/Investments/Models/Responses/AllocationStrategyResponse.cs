using Babylon.Alfred.Api.Features.Investments.Models.Requests;

namespace Babylon.Alfred.Api.Features.Investments.Models.Responses;

public class AllocationStrategyResponse
{
    public List<AllocationStrategyDto> Allocations { get; set; } = [];
    public decimal TotalAllocated { get; set; }
}

