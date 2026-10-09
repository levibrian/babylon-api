using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface IAllocationService
{
    Task<IList<AllocationTargetDto>> GetTargets(Guid userId);
    Task<IList<AllocationTargetDto>> ReplaceTargets(Guid userId, UpdateAllocationTargetsRequest request);
}
