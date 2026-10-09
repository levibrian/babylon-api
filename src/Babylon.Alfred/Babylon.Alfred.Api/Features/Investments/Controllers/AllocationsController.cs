using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Controllers;
using Babylon.Alfred.Api.Shared.Extensions;
using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[Authorize]
[Route("api/v1/allocations")]
public class AllocationsController(IAllocationService allocationService) : BabylonControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IList<AllocationTargetDto>>>> Get()
    {
        var targets = await allocationService.GetTargets(User.GetUserId());
        return Success(targets);
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<IList<AllocationTargetDto>>>> Put([FromBody] UpdateAllocationTargetsRequest request)
    {
        var targets = await allocationService.ReplaceTargets(User.GetUserId(), request);
        return Success(targets);
    }
}
