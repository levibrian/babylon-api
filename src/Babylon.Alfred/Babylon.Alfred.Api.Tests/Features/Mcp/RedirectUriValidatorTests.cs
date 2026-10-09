using Babylon.Alfred.Api.Features.Mcp.Services;
using FluentAssertions;

namespace Babylon.Alfred.Api.Tests.Features.Mcp;

public class RedirectUriValidatorTests
{
    private static readonly string[] Allowlist =
    [
        "https://claude.ai/api/mcp/auth_callback",
        "https://chatgpt.com/connector_platform_oauth_redirect",
        "http://localhost/callback",
        "http://127.0.0.1/callback"
    ];

    [Theory]
    [InlineData("https://claude.ai/api/mcp/auth_callback")]
    [InlineData("https://chatgpt.com/connector_platform_oauth_redirect")]
    [InlineData("http://localhost:3118/callback")]
    [InlineData("http://localhost/callback")]
    [InlineData("http://127.0.0.1:54321/callback")]
    public void IsAllowed_WithAllowlistedUri_ReturnsTrue(string uri)
    {
        RedirectUriValidator.IsAllowed(uri, Allowlist).Should().BeTrue();
    }

    [Theory]
    [InlineData("https://evil.example/callback")]
    [InlineData("https://claude.ai/api/mcp/auth_callback.evil.example")]
    [InlineData("https://claude.ai/api/mcp/auth_callback/../other")]
    [InlineData("https://claude.ai/api/mcp/auth_callback?next=https://evil.example")]
    [InlineData("https://claude.ai/api/mcp/auth_callback#frag")]
    [InlineData("http://claude.ai/api/mcp/auth_callback")]
    [InlineData("https://CLAUDE.ai/api/mcp/auth_callback")]
    [InlineData("http://localhost:3118/other")]
    [InlineData("http://localhost:3118/callback?x=1")]
    [InlineData("https://localhost:3118/callback")]
    [InlineData("http://localhost@evil.example/callback")]
    [InlineData("http://evil.example:3118/callback")]
    [InlineData("not a uri")]
    [InlineData(null)]
    public void IsAllowed_WithUriOutsideAllowlist_ReturnsFalse(string? uri)
    {
        RedirectUriValidator.IsAllowed(uri, Allowlist).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_WithEmptyAllowlist_ReturnsFalse()
    {
        RedirectUriValidator.IsAllowed("https://claude.ai/api/mcp/auth_callback", []).Should().BeFalse();
    }
}
