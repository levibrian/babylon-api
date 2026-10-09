using Babylon.Alfred.Api.Features.Mcp.Services;
using FluentAssertions;

namespace Babylon.Alfred.Api.Tests.Features.Mcp;

public class PkceTests
{
    // RFC 7636 Appendix B
    private const string Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
    private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

    [Fact]
    public void VerifyS256_WithMatchingVerifier_ReturnsTrue()
    {
        Pkce.VerifyS256(Verifier, Challenge).Should().BeTrue();
    }

    [Theory]
    [InlineData("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXX")]
    [InlineData("too-short")]
    [InlineData(null)]
    public void VerifyS256_WithWrongOrInvalidVerifier_ReturnsFalse(string? verifier)
    {
        Pkce.VerifyS256(verifier, Challenge).Should().BeFalse();
    }

    [Fact]
    public void VerifyS256_WithPlainChallenge_ReturnsFalse()
    {
        Pkce.VerifyS256(Verifier, Verifier).Should().BeFalse();
    }

    [Theory]
    [InlineData(Challenge, true)]
    [InlineData("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-c=", false)]
    [InlineData("short", false)]
    [InlineData(null, false)]
    public void IsValidChallenge_ChecksBase64UrlSha256Shape(string? challenge, bool expected)
    {
        Pkce.IsValidChallenge(challenge).Should().Be(expected);
    }
}
