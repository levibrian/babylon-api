using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Mcp.Tools;
using FluentAssertions;
using Moq;

namespace Babylon.Alfred.Api.Tests.Mcp;

public class CashToolTests
{
    [Fact]
    public async Task SetCashBalance_CallsServiceWithCallerUserId()
    {
        var userId = Guid.NewGuid();
        var user = Principal(userId);
        var cashBalanceService = new Mock<ICashBalanceService>();

        await CashTool.SetCashBalance(user, cashBalanceService.Object, 0m);

        cashBalanceService.Verify(x => x.UpdateManualBalanceAsync(userId, 0m), Times.Once);
    }

    [Fact]
    public async Task SetCashBalance_WithoutAuthenticatedUser_Throws()
    {
        var cashBalanceService = new Mock<ICashBalanceService>();

        Func<Task> act = () => CashTool.SetCashBalance(new ClaimsPrincipal(new ClaimsIdentity()), cashBalanceService.Object, 0m);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static ClaimsPrincipal Principal(Guid userId)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
}
