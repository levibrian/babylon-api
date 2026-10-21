using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Mcp.Tools;
using FluentAssertions;
using Moq;

namespace Babylon.Alfred.Api.Tests.Mcp;

public class TargetsToolTests
{
    [Fact]
    public async Task GetTargets_ReturnsServiceResultForCallersUserId()
    {
        var userId = Guid.NewGuid();
        var user = Principal(userId);
        var expected = new List<AllocationTargetDto> { new() };
        var allocationService = new Mock<IAllocationService>();
        allocationService.Setup(x => x.GetTargets(userId)).ReturnsAsync(expected);

        var result = await TargetsTool.GetTargets(user, allocationService.Object);

        result.Should().BeSameAs(expected);
        allocationService.Verify(x => x.GetTargets(userId), Times.Once);
    }

    [Fact]
    public async Task GetTargets_WithoutAuthenticatedUser_Throws()
    {
        var allocationService = new Mock<IAllocationService>();

        Func<Task> act = () => TargetsTool.GetTargets(new ClaimsPrincipal(new ClaimsIdentity()), allocationService.Object);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SetAllocationTargets_ReturnsServiceResultForCallersUserId()
    {
        var userId = Guid.NewGuid();
        var user = Principal(userId);
        var request = new UpdateAllocationTargetsRequest
        {
            Targets = [new AllocationTargetRequest { Ticker = "VWCE", TargetPercentage = 100m }]
        };
        var expected = new List<AllocationTargetDto> { new() };
        var allocationService = new Mock<IAllocationService>();
        allocationService.Setup(x => x.ReplaceTargets(userId, request)).ReturnsAsync(expected);

        var result = await TargetsTool.SetAllocationTargets(user, allocationService.Object, request);

        result.Should().BeSameAs(expected);
        allocationService.Verify(x => x.ReplaceTargets(userId, request), Times.Once);
    }

    [Fact]
    public async Task SetAllocationTargets_WithoutAuthenticatedUser_Throws()
    {
        var allocationService = new Mock<IAllocationService>();

        Func<Task> act = () => TargetsTool.SetAllocationTargets(
            new ClaimsPrincipal(new ClaimsIdentity()), allocationService.Object, new UpdateAllocationTargetsRequest());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static ClaimsPrincipal Principal(Guid userId)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
}
