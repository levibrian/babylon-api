using Babylon.Alfred.Api.Features.Mcp.Models.Requests;
using Babylon.Alfred.Api.Features.Mcp.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Babylon.Alfred.Api.Tests.Features.Mcp;

public class ClientRegistrationServiceTests
{
    private readonly ClientRegistrationService sut = new(McpTestOptions.Create(), McpTestOptions.Configuration());

    [Fact]
    public void Register_WithAllowlistedRedirect_ReturnsClientThatRoundTrips()
    {
        var result = sut.Register(new ClientRegistrationRequest
        {
            ClientName = "Claude",
            RedirectUris = [McpTestOptions.ClaudeCallback]
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TokenEndpointAuthMethod.Should().Be("none");

        var client = sut.GetClient(result.Value.ClientId);
        client.Should().NotBeNull();
        client!.ClientName.Should().Be("Claude");
        client.RedirectUris.Should().Equal(McpTestOptions.ClaudeCallback);
    }

    [Fact]
    public void Register_WithAnyRedirectOutsideAllowlist_IsRejected()
    {
        var result = sut.Register(new ClientRegistrationRequest
        {
            RedirectUris = [McpTestOptions.ClaudeCallback, "https://evil.example/callback"]
        });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("invalid_redirect_uri");
    }

    [Fact]
    public void Register_WithoutRedirectUris_IsRejected()
    {
        sut.Register(new ClientRegistrationRequest()).Error.Should().Be("invalid_redirect_uri");
    }

    [Fact]
    public void Register_WithUnsupportedGrantType_IsRejected()
    {
        var result = sut.Register(new ClientRegistrationRequest
        {
            RedirectUris = [McpTestOptions.ClaudeCallback],
            GrantTypes = ["client_credentials"]
        });

        result.Error.Should().Be("invalid_client_metadata");
    }

    [Fact]
    public void GetClient_WithTamperedClientId_ReturnsNull()
    {
        var clientId = sut.Register(new ClientRegistrationRequest { RedirectUris = [McpTestOptions.ClaudeCallback] }).Value!.ClientId;
        var forged = sut.Register(new ClientRegistrationRequest { RedirectUris = ["http://localhost:1234/callback"] }).Value!.ClientId;

        var tampered = $"{forged.Split('.')[0]}.{clientId.Split('.')[1]}";

        sut.GetClient(tampered).Should().BeNull();
        sut.GetClient("garbage").Should().BeNull();
        sut.GetClient(null).Should().BeNull();
    }

    [Fact]
    public void GetClient_SignedWithDifferentSecret_ReturnsNull()
    {
        var clientId = sut.Register(new ClientRegistrationRequest { RedirectUris = [McpTestOptions.ClaudeCallback] }).Value!.ClientId;
        var otherConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Jwt:SecretKey"] = "another-secret-key-0123456789-abcdefghijklmnop" })
            .Build();

        new ClientRegistrationService(McpTestOptions.Create(), otherConfig).GetClient(clientId).Should().BeNull();
    }
}
