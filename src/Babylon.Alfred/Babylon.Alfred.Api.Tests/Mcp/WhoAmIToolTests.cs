using System.Security.Claims;
using Babylon.Alfred.Mcp.Tools;
using FluentAssertions;

namespace Babylon.Alfred.Api.Tests.Mcp;

public class WhoAmIToolTests
{
    [Fact]
    public void WhoAmI_ReturnsOnlyTheCallersIdentity()
    {
        var alice = Principal(Guid.NewGuid(), "alice@example.com");
        var bob = Principal(Guid.NewGuid(), "bob@example.com");

        WhoAmITool.WhoAmI(alice).Should().Be(new WhoAmIResult(Guid.Parse(alice.FindFirstValue(ClaimTypes.NameIdentifier)!), "alice@example.com"));
        WhoAmITool.WhoAmI(bob).Should().Be(new WhoAmIResult(Guid.Parse(bob.FindFirstValue(ClaimTypes.NameIdentifier)!), "bob@example.com"));
    }

    [Fact]
    public void WhoAmI_WithoutAuthenticatedUser_Throws()
    {
        var act = () => WhoAmITool.WhoAmI(new ClaimsPrincipal(new ClaimsIdentity()));
        act.Should().Throw<UnauthorizedAccessException>();
    }

    private static ClaimsPrincipal Principal(Guid userId, string email)
        => new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Email, email)],
            "test"));
}
