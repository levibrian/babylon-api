using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Mcp.Tools;
using FluentAssertions;
using Moq;

namespace Babylon.Alfred.Api.Tests.Mcp;

public class PortfolioToolTests
{
    [Fact]
    public async Task GetPortfolio_ReturnsServiceResultForCallersUserId()
    {
        var userId = Guid.NewGuid();
        var user = Principal(userId);
        var expected = new PortfolioResponse();
        var portfolioService = new Mock<IPortfolioService>();
        portfolioService.Setup(x => x.GetPortfolio(userId)).ReturnsAsync(expected);

        var result = await PortfolioTool.GetPortfolio(user, portfolioService.Object);

        result.Should().BeSameAs(expected);
        portfolioService.Verify(x => x.GetPortfolio(userId), Times.Once);
    }

    [Fact]
    public async Task GetPortfolio_WithoutAuthenticatedUser_Throws()
    {
        var portfolioService = new Mock<IPortfolioService>();

        Func<Task> act = () => PortfolioTool.GetPortfolio(new ClaimsPrincipal(new ClaimsIdentity()), portfolioService.Object);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static ClaimsPrincipal Principal(Guid userId)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
}
