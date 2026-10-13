using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Babylon.Alfred.Api.Features.Authentication.Utils;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Mcp.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Babylon.Alfred.Api.Tests.Mcp;

public class McpEndpointTests(McpEndpointTests.Factory factory) : IClassFixture<McpEndpointTests.Factory>
{
    private const string ExpectedChallenge = "Bearer resource_metadata=\"http://localhost/.well-known/oauth-protected-resource/mcp\"";

    [Fact]
    public async Task PostMcp_WithoutToken_Returns401WithResourceMetadataChallenge()
    {
        var response = await factory.CreateClient().SendAsync(ToolsCall(token: null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().Should().Be(ExpectedChallenge);
    }

    [Fact]
    public async Task PostMcp_WithRestApiToken_Returns401()
    {
        var restToken = factory.Services.GetRequiredService<JwtTokenGenerator>().GenerateToken(TestUser());

        var response = await factory.CreateClient().SendAsync(ToolsCall(restToken));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostMcp_WhoAmI_ReturnsTheCallersIdentity()
    {
        var user = TestUser();
        var token = factory.Services.GetRequiredService<JwtTokenGenerator>()
            .GenerateToken(user, "http://localhost", "http://localhost/mcp", 5);

        var response = await factory.CreateClient().SendAsync(ToolsCall(token));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(user.Id.ToString()).And.Contain(user.Email);
    }

    [Fact]
    public async Task ProtectedResourceMetadata_PointsAtThisAuthorizationServer()
    {
        var json = JObject.Parse(await factory.CreateClient().GetStringAsync("/.well-known/oauth-protected-resource/mcp"));

        json["resource"]!.ToString().Should().Be("http://localhost/mcp");
        json["authorization_servers"]!.Values<string>().Should().Equal("http://localhost");
    }

    [Fact]
    public async Task AuthorizationServerMetadata_AdvertisesPkceS256AndRegistration()
    {
        var json = JObject.Parse(await factory.CreateClient().GetStringAsync("/.well-known/oauth-authorization-server"));

        json["issuer"]!.ToString().Should().Be("http://localhost");
        json["code_challenge_methods_supported"]!.Values<string>().Should().Equal("S256");
        json["registration_endpoint"]!.ToString().Should().Be("http://localhost/register");
        json["token_endpoint_auth_methods_supported"]!.Values<string>().Should().Equal("none");
    }

    [Fact]
    public async Task Register_WithDisallowedRedirect_Returns400()
    {
        var response = await factory.CreateClient().PostAsync("/register",
            new StringContent("{\"redirect_uris\":[\"https://evil.example/cb\"]}", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("invalid_redirect_uri");
    }

    [Fact]
    public async Task Register_ThenAuthorize_ShowsGoogleSignInPage()
    {
        var client = factory.CreateClient();
        var registration = await client.PostAsync("/register",
            new StringContent("{\"client_name\":\"Claude\",\"redirect_uris\":[\"https://claude.ai/api/mcp/auth_callback\"]}", Encoding.UTF8, "application/json"));
        registration.StatusCode.Should().Be(HttpStatusCode.Created);
        var clientId = JObject.Parse(await registration.Content.ReadAsStringAsync())["client_id"]!.ToString();

        var page = await client.GetAsync(
            $"/authorize?response_type=code&client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString("https://claude.ai/api/mcp/auth_callback")}" +
            "&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256&state=xyz&resource=http%3A%2F%2Flocalhost%2Fmcp");

        page.StatusCode.Should().Be(HttpStatusCode.OK);
        (await page.Content.ReadAsStringAsync()).Should().Contain("accounts.google.com/gsi/client").And.Contain("claude.ai");
    }

    [Fact]
    public async Task RestApiRoutes_AreNotExposedOnTheMcpHost()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/portfolios");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static User TestUser() => new() { Id = Guid.NewGuid(), Email = "brian@example.com", Username = "brian" };

    private static HttpRequestMessage ToolsCall(string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"whoami\",\"arguments\":{}}}",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    public class Factory : WebApplicationFactory<McpOptions>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Authentication:Jwt:SecretKey", McpTestOptions.SecretKey);
            builder.UseSetting("Mcp:BaseUrl", "http://localhost");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=unused");
        }
    }
}
