using System.ComponentModel;
using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using ModelContextProtocol.Server;

namespace Babylon.Alfred.Mcp.Tools;

[McpServerToolType]
public class TargetsTool
{
    [McpServerTool(Name = "get_targets", ReadOnly = true)]
    [Description("Returns the signed-in Babylon user's target allocation percentage per security.")]
    public static Task<IList<AllocationTargetDto>> GetTargets(ClaimsPrincipal? user, IAllocationService allocationService)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }

        return allocationService.GetTargets(user.GetUserId());
    }

    [McpServerTool(Name = "set_allocation_targets", Idempotent = true)]
    [Description("Replaces the signed-in Babylon user's full set of target allocations. Only call this after the user has explicitly confirmed the targets in conversation — this is a separate confirmation from confirming their investor profile.")]
    public static Task<IList<AllocationTargetDto>> SetAllocationTargets(
        ClaimsPrincipal? user, IAllocationService allocationService, UpdateAllocationTargetsRequest request)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }

        return allocationService.ReplaceTargets(user.GetUserId(), request);
    }
}
