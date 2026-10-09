using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Babylon.Alfred.Api.Features.Mcp.Models;
using Babylon.Alfred.Api.Features.Mcp.Models.Requests;
using Babylon.Alfred.Api.Features.Mcp.Models.Responses;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Babylon.Alfred.Api.Features.Mcp.Services;

// Stateless DCR: the client_id is the signed registration itself, so nothing is stored
// and registrations survive restarts. Rotating the JWT secret invalidates all client_ids.
public class ClientRegistrationService(IOptions<McpOptions> options, IConfiguration configuration) : IClientRegistrationService
{
    private const int MaxClientNameLength = 100;
    private const int MaxRedirectUris = 10;
    private static readonly string[] SupportedGrantTypes = ["authorization_code", "refresh_token"];

    public OAuthResult<ClientRegistrationResponse> Register(ClientRegistrationRequest request)
    {
        var redirectUris = request.RedirectUris?.Distinct().ToList() ?? [];

        if (redirectUris.Count == 0 || redirectUris.Count > MaxRedirectUris)
        {
            return OAuthResult<ClientRegistrationResponse>.Fail("invalid_redirect_uri", "redirect_uris is required");
        }

        if (redirectUris.Any(uri => !RedirectUriValidator.IsAllowed(uri, options.Value.AllowedRedirectUris)))
        {
            return OAuthResult<ClientRegistrationResponse>.Fail("invalid_redirect_uri", "redirect_uri is not allowed");
        }

        if (request.GrantTypes is { Count: > 0 } && request.GrantTypes.Any(g => !SupportedGrantTypes.Contains(g)))
        {
            return OAuthResult<ClientRegistrationResponse>.Fail("invalid_client_metadata", "unsupported grant_type");
        }

        if (request.ResponseTypes is { Count: > 0 } && request.ResponseTypes.Any(r => r != "code"))
        {
            return OAuthResult<ClientRegistrationResponse>.Fail("invalid_client_metadata", "unsupported response_type");
        }

        var name = string.IsNullOrWhiteSpace(request.ClientName) ? "MCP client" : request.ClientName.Trim();
        if (name.Length > MaxClientNameLength)
        {
            name = name[..MaxClientNameLength];
        }

        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = Base64UrlEncoder.Encode(JsonSerializer.SerializeToUtf8Bytes(new ClientPayload(redirectUris, name, issuedAt)));
        var clientId = $"{payload}.{Sign(payload)}";

        return OAuthResult<ClientRegistrationResponse>.Ok(new ClientRegistrationResponse
        {
            ClientId = clientId,
            ClientIdIssuedAt = issuedAt,
            ClientName = name,
            RedirectUris = redirectUris
        });
    }

    public RegisteredClient? GetClient(string? clientId)
    {
        var parts = clientId?.Split('.');
        if (parts is not { Length: 2 })
        {
            return null;
        }

        var expected = Encoding.ASCII.GetBytes(Sign(parts[0]));
        if (!CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(parts[1])))
        {
            return null;
        }

        var payload = JsonSerializer.Deserialize<ClientPayload>(Base64UrlEncoder.DecodeBytes(parts[0]));
        return payload is null ? null : new RegisteredClient(clientId!, payload.N, payload.R, payload.T);
    }

    private string Sign(string payload)
    {
        var secret = configuration["Authentication:Jwt:SecretKey"]
                     ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        var key = SHA256.HashData(Encoding.UTF8.GetBytes($"babylon-mcp-client-id:{secret}"));
        return Base64UrlEncoder.Encode(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload)));
    }

    private record ClientPayload(List<string> R, string N, long T);
}
