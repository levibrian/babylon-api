using System.ComponentModel;
using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using ModelContextProtocol.Server;

namespace Babylon.Alfred.Mcp.Tools;

[McpServerToolType]
public class CashTool
{
    [McpServerTool(Name = "set_cash_balance")]
    [Description("Sets the signed-in Babylon user's cash balance to the given amount.")]
    public static Task SetCashBalance(ClaimsPrincipal? user, ICashBalanceService cashBalanceService, decimal amount)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }

        return cashBalanceService.UpdateManualBalanceAsync(user.GetUserId(), amount);
    }
}
