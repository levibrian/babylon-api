using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Mcp.Tools;
using FluentAssertions;
using Moq;

namespace Babylon.Alfred.Api.Tests.Mcp;

public class RebalancingToolTests
{
    [Fact]
    public async Task GetRebalancing_ReturnsRebalancingFieldsPerPositionForCallersUserId()
    {
        var userId = Guid.NewGuid();
        var user = Principal(userId);
        var portfolio = new PortfolioResponse
        {
            Positions =
            [
                new PortfolioPositionDto
                {
                    Ticker = "VWCE",
                    CurrentAllocationPercentage = 40m,
                    TargetAllocationPercentage = 50m,
                    AllocationDeviation = -10m,
                    RebalancingAmount = 500m,
                    RebalancingStatus = RebalancingStatus.Underweight
                }
            ]
        };
        var portfolioService = new Mock<IPortfolioService>();
        portfolioService.Setup(x => x.GetPortfolio(userId)).ReturnsAsync(portfolio);

        var result = await RebalancingTool.GetRebalancing(user, portfolioService.Object);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new RebalancingLineDto
        {
            Ticker = "VWCE",
            CurrentAllocationPercentage = 40m,
            TargetAllocationPercentage = 50m,
            AllocationDeviation = -10m,
            RebalancingAmount = 500m,
            RebalancingStatus = RebalancingStatus.Underweight
        });
    }

    [Fact]
    public async Task GetRebalancing_WithoutAuthenticatedUser_Throws()
    {
        var portfolioService = new Mock<IPortfolioService>();

        Func<Task> act = () => RebalancingTool.GetRebalancing(new ClaimsPrincipal(new ClaimsIdentity()), portfolioService.Object);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static ClaimsPrincipal Principal(Guid userId)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
}
