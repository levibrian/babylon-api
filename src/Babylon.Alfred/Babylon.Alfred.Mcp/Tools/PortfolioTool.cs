using System.ComponentModel;
using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using ModelContextProtocol.Server;

namespace Babylon.Alfred.Mcp.Tools;

[McpServerToolType]
public class PortfolioTool
{
    [McpServerTool(Name = "get_portfolio", ReadOnly = true)]
    [Description("Returns the signed-in Babylon user's portfolio: positions, cash, and rebalancing status per position.")]
    public static Task<PortfolioResponse> GetPortfolio(ClaimsPrincipal? user, IPortfolioService portfolioService)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }

        return portfolioService.GetPortfolio(user.GetUserId());
    }
}
