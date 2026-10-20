using System.ComponentModel;
using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using ModelContextProtocol.Server;

namespace Babylon.Alfred.Mcp.Tools;

[McpServerToolType]
public class RebalancingTool
{
    [McpServerTool(Name = "get_rebalancing", ReadOnly = true)]
    [Description("Returns what to buy or sell per security to hit the signed-in Babylon user's target allocations.")]
    public static async Task<IList<RebalancingLineDto>> GetRebalancing(ClaimsPrincipal? user, IPortfolioService portfolioService)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }

        var portfolio = await portfolioService.GetPortfolio(user.GetUserId());

        return portfolio.Positions.Select(p => new RebalancingLineDto
        {
            Ticker = p.Ticker,
            CurrentAllocationPercentage = p.CurrentAllocationPercentage,
            TargetAllocationPercentage = p.TargetAllocationPercentage,
            AllocationDeviation = p.AllocationDeviation,
            RebalancingAmount = p.RebalancingAmount,
            RebalancingStatus = p.RebalancingStatus
        }).ToList();
    }
}

public class RebalancingLineDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal? CurrentAllocationPercentage { get; set; }
    public decimal? TargetAllocationPercentage { get; set; }
    public decimal? AllocationDeviation { get; set; }
    public decimal? RebalancingAmount { get; set; }
    public RebalancingStatus? RebalancingStatus { get; set; }
}
