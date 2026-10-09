using Babylon.Alfred.Api.Features.Mcp.Models;
using Babylon.Alfred.Api.Features.Mcp.Services;
using FluentAssertions;

namespace Babylon.Alfred.Api.Tests.Features.Mcp;

public class AuthorizationStoreTests
{
    private static readonly PendingAuthorization Pending =
        new("client", "Claude", McpTestOptions.ClaudeCallback, "challenge", "state");

    [Fact]
    public void RedeemCode_IsSingleUse()
    {
        var sut = new AuthorizationStore(McpTestOptions.Create());
        var userId = Guid.NewGuid();
        var code = sut.IssueCode(Pending, userId);

        var first = sut.RedeemCode(code);
        var second = sut.RedeemCode(code);

        first.Should().NotBeNull();
        first!.UserId.Should().Be(userId);
        first.ClientId.Should().Be("client");
        second.Should().BeNull();
    }

    [Fact]
    public void RedeemCode_WhenExpired_ReturnsNull()
    {
        var sut = new AuthorizationStore(McpTestOptions.Create(codeSeconds: 0));
        var code = sut.IssueCode(Pending, Guid.NewGuid());

        sut.RedeemCode(code).Should().BeNull();
    }

    [Fact]
    public void RedeemCode_WithUnknownCode_ReturnsNull()
    {
        var sut = new AuthorizationStore(McpTestOptions.Create());
        sut.RedeemCode("unknown").Should().BeNull();
        sut.RedeemCode(null).Should().BeNull();
    }

    [Fact]
    public void TakePending_IsSingleUse()
    {
        var sut = new AuthorizationStore(McpTestOptions.Create());
        var id = sut.SavePending(Pending);

        sut.TakePending(id).Should().Be(Pending);
        sut.TakePending(id).Should().BeNull();
    }
}
