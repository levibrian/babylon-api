using System.ComponentModel;
using System.Security.Claims;
using Babylon.Alfred.Api.Shared.Extensions;
using ModelContextProtocol.Server;

namespace Babylon.Alfred.Mcp.Tools;

[McpServerToolType]
public class WhoAmITool
{
    [McpServerTool(Name = "whoami", ReadOnly = true, Idempotent = true)]
    [Description("Returns the id and email of the signed-in Babylon user.")]
    public static WhoAmIResult WhoAmI(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }

        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? string.Empty;
        return new WhoAmIResult(user.GetUserId(), email);
    }
}

public record WhoAmIResult(Guid UserId, string Email);
