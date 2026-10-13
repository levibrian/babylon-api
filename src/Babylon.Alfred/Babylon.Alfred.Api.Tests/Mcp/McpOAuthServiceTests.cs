using System.IdentityModel.Tokens.Jwt;
using Babylon.Alfred.Api.Features.Authentication.Services;
using Babylon.Alfred.Api.Features.Authentication.Utils;
using Babylon.Alfred.Mcp.Models;
using Babylon.Alfred.Mcp.Models.Requests;
using Babylon.Alfred.Mcp.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Babylon.Alfred.Api.Tests.Mcp;

public class McpOAuthServiceTests
{
    private const string Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
    private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

    private readonly User user = new() { Id = Guid.NewGuid(), Email = "brian@example.com", Username = "brian" };
    private readonly Mock<IAuthService> authService = new();
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IRefreshTokenRepository> refreshTokenRepository = new();
    private readonly ClientRegistrationService clients = new(McpTestOptions.Create(), McpTestOptions.Configuration());
    private readonly McpOAuthService sut;
    private readonly string clientId;

    public McpOAuthServiceTests()
    {
        authService.Setup(x => x.AuthenticateGoogleUserAsync("good-id-token")).ReturnsAsync(user);
        authService.Setup(x => x.AuthenticateGoogleUserAsync(It.Is<string>(t => t != "good-id-token")))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid Google token"));
        userRepository.Setup(x => x.GetUserAsync(user.Id)).ReturnsAsync(user);

        sut = new McpOAuthService(
            clients,
            new AuthorizationStore(McpTestOptions.Create()),
            authService.Object,
            userRepository.Object,
            refreshTokenRepository.Object,
            new JwtTokenGenerator(McpTestOptions.Configuration()),
            McpTestOptions.Create(),
            NullLogger<McpOAuthService>.Instance);

        clientId = clients.Register(new ClientRegistrationRequest
        {
            ClientName = "Claude",
            RedirectUris = [McpTestOptions.ClaudeCallback, "http://localhost:3118/callback"]
        }).Value!.ClientId;
    }

    [Fact]
    public async Task FullFlow_IssuesAccessTokenForTheResource()
    {
        var code = await SignIn();

        var result = await sut.ExchangeToken(CodeRequest(code));

        result.IsSuccess.Should().BeTrue();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value!.AccessToken);
        jwt.Audiences.Should().Equal("https://babylon.test/mcp");
        jwt.Issuer.Should().Be("https://babylon.test");
        jwt.Subject.Should().Be(user.Id.ToString());
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        refreshTokenRepository.Verify(x => x.AddAsync(It.Is<RefreshToken>(t => t.UserId == user.Id)), Times.Once);
        refreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExchangeToken_ReusingCode_IsRejected()
    {
        var code = await SignIn();
        (await sut.ExchangeToken(CodeRequest(code))).IsSuccess.Should().BeTrue();

        var second = await sut.ExchangeToken(CodeRequest(code));

        second.Error.Should().Be("invalid_grant");
    }

    [Theory]
    [InlineData("wrong-verifier-wrong-verifier-wrong-verifier-123")]
    [InlineData(null)]
    public async Task ExchangeToken_WithBadVerifier_IsRejected(string? verifier)
    {
        var code = await SignIn();
        var request = CodeRequest(code);
        request.CodeVerifier = verifier;

        (await sut.ExchangeToken(request)).Error.Should().Be("invalid_grant");
    }

    [Fact]
    public async Task ExchangeToken_WithDifferentRedirectUri_IsRejected()
    {
        var code = await SignIn();
        var request = CodeRequest(code);
        request.RedirectUri = "http://localhost:3118/callback";

        (await sut.ExchangeToken(request)).Error.Should().Be("invalid_grant");
    }

    [Fact]
    public async Task ExchangeToken_FromAnotherClient_IsRejected()
    {
        var code = await SignIn();
        var otherClient = clients.Register(new ClientRegistrationRequest { RedirectUris = [McpTestOptions.ClaudeCallback] }).Value!.ClientId;
        var request = CodeRequest(code);
        request.ClientId = otherClient;

        (await sut.ExchangeToken(request)).Error.Should().Be("invalid_grant");
    }

    [Fact]
    public async Task ExchangeToken_ForAnotherResource_IsRejected()
    {
        var code = await SignIn();
        var request = CodeRequest(code);
        request.Resource = "https://other.example/mcp";

        (await sut.ExchangeToken(request)).Error.Should().Be("invalid_target");
    }

    [Fact]
    public async Task ExchangeToken_WithRefreshToken_RotatesIt()
    {
        var stored = new RefreshToken { Token = "refresh", User = user, UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        refreshTokenRepository.Setup(x => x.GetByTokenAsync("refresh")).ReturnsAsync(stored);

        var result = await sut.ExchangeToken(new TokenRequest { GrantType = "refresh_token", RefreshToken = "refresh", ClientId = clientId });

        result.IsSuccess.Should().BeTrue();
        stored.IsRevoked.Should().BeTrue();
        result.Value!.RefreshToken.Should().NotBe("refresh");
    }

    [Fact]
    public async Task ExchangeToken_WithRevokedRefreshToken_IsRejected()
    {
        refreshTokenRepository.Setup(x => x.GetByTokenAsync("refresh"))
            .ReturnsAsync(new RefreshToken { Token = "refresh", User = user, IsRevoked = true, ExpiresAt = DateTime.UtcNow.AddDays(1) });

        var result = await sut.ExchangeToken(new TokenRequest { GrantType = "refresh_token", RefreshToken = "refresh", ClientId = clientId });

        result.Error.Should().Be("invalid_grant");
    }

    [Fact]
    public void Authorize_WithUnknownClient_ShowsErrorWithoutRedirect()
    {
        var outcome = sut.Authorize(AuthorizeRequest());
        outcome.Should().BeOfType<AuthorizeOutcome.ShowSignIn>();

        var request = AuthorizeRequest();
        request.ClientId = "forged.client";
        sut.Authorize(request).Should().BeOfType<AuthorizeOutcome.ShowError>();
    }

    [Theory]
    [InlineData("https://evil.example/callback")]
    [InlineData("http://localhost:3118/other")]
    public void Authorize_WithUnregisteredRedirectUri_ShowsErrorWithoutRedirect(string redirectUri)
    {
        var request = AuthorizeRequest();
        request.RedirectUri = redirectUri;

        sut.Authorize(request).Should().BeOfType<AuthorizeOutcome.ShowError>();
    }

    [Fact]
    public void Authorize_WithLoopbackOnDifferentPort_IsAccepted()
    {
        var request = AuthorizeRequest();
        request.RedirectUri = "http://localhost:50123/callback";

        sut.Authorize(request).Should().BeOfType<AuthorizeOutcome.ShowSignIn>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("plain")]
    public void Authorize_WithoutS256_RedirectsWithInvalidRequest(string? method)
    {
        var request = AuthorizeRequest();
        request.CodeChallengeMethod = method;

        var redirect = sut.Authorize(request).Should().BeOfType<AuthorizeOutcome.RedirectTo>().Subject;
        var query = QueryHelpers.ParseQuery(new Uri(redirect.Url).Query);
        query["error"].ToString().Should().Be("invalid_request");
        query["state"].ToString().Should().Be("xyz");
        query["iss"].ToString().Should().Be("https://babylon.test");
    }

    [Fact]
    public async Task CompleteGoogleSignIn_WithInvalidGoogleToken_RedirectsWithAccessDenied()
    {
        var signIn = (AuthorizeOutcome.ShowSignIn)sut.Authorize(AuthorizeRequest());

        var outcome = await sut.CompleteGoogleSignIn(signIn.RequestId, "bad-token");

        var redirect = outcome.Should().BeOfType<AuthorizeOutcome.RedirectTo>().Subject;
        redirect.Url.Should().StartWith(McpTestOptions.ClaudeCallback).And.Contain("error=access_denied").And.NotContain("code=");
    }

    [Fact]
    public async Task CompleteGoogleSignIn_WithUnknownRequest_ShowsError()
    {
        (await sut.CompleteGoogleSignIn("unknown", "good-id-token")).Should().BeOfType<AuthorizeOutcome.ShowError>();
    }

    private async Task<string> SignIn()
    {
        var signIn = sut.Authorize(AuthorizeRequest()).Should().BeOfType<AuthorizeOutcome.ShowSignIn>().Subject;
        var redirect = (await sut.CompleteGoogleSignIn(signIn.RequestId, "good-id-token"))
            .Should().BeOfType<AuthorizeOutcome.RedirectTo>().Subject;

        redirect.Url.Should().StartWith(McpTestOptions.ClaudeCallback + "?");
        var query = QueryHelpers.ParseQuery(new Uri(redirect.Url).Query);
        query["state"].ToString().Should().Be("xyz");
        query["iss"].ToString().Should().Be("https://babylon.test");
        return query["code"].ToString();
    }

    private AuthorizeRequest AuthorizeRequest() => new()
    {
        ResponseType = "code",
        ClientId = clientId,
        RedirectUri = McpTestOptions.ClaudeCallback,
        CodeChallenge = Challenge,
        CodeChallengeMethod = "S256",
        State = "xyz",
        Resource = "https://babylon.test/mcp"
    };

    private TokenRequest CodeRequest(string code) => new()
    {
        GrantType = "authorization_code",
        Code = code,
        ClientId = clientId,
        RedirectUri = McpTestOptions.ClaudeCallback,
        CodeVerifier = Verifier,
        Resource = "https://babylon.test/mcp"
    };
}
