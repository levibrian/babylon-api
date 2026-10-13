using Babylon.Alfred.Api.Features.Authentication.Services;
using Babylon.Alfred.Api.Features.Authentication.Utils;
using Babylon.Alfred.Mcp.Models;
using Babylon.Alfred.Mcp.Models.Requests;
using Babylon.Alfred.Mcp.Models.Responses;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Logging;
using Babylon.Alfred.Api.Shared.Repositories;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Babylon.Alfred.Mcp.Services;

public class McpOAuthService(
    IClientRegistrationService clientRegistrationService,
    AuthorizationStore authorizationStore,
    IAuthService authService,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    JwtTokenGenerator jwtTokenGenerator,
    IOptions<McpOptions> options,
    ILogger<McpOAuthService> logger) : IMcpOAuthService
{
    private McpOptions Options => options.Value;

    public AuthorizationServerMetadataResponse GetMetadata() => new()
    {
        Issuer = Options.Issuer,
        AuthorizationEndpoint = $"{Options.Issuer}/authorize",
        TokenEndpoint = $"{Options.Issuer}/token",
        RegistrationEndpoint = $"{Options.Issuer}/register"
    };

    public AuthorizeOutcome Authorize(AuthorizeRequest request)
    {
        var client = clientRegistrationService.GetClient(request.ClientId);
        if (client is null)
        {
            return new AuthorizeOutcome.ShowError("Unknown client.");
        }

        var redirectUri = request.RedirectUri ?? (client.RedirectUris.Count == 1 ? client.RedirectUris[0] : null);
        if (redirectUri is null
            || !RedirectUriValidator.IsAllowed(redirectUri, client.RedirectUris)
            || !RedirectUriValidator.IsAllowed(redirectUri, Options.AllowedRedirectUris))
        {
            logger.LogValidationFailure("McpAuthorize", "redirect_uri rejected", new { client.ClientName });
            return new AuthorizeOutcome.ShowError("Invalid redirect_uri.");
        }

        if (request.ResponseType != "code")
        {
            return RedirectWithError(redirectUri, request.State, "unsupported_response_type");
        }

        if (request.CodeChallengeMethod != "S256" || !Pkce.IsValidChallenge(request.CodeChallenge))
        {
            return RedirectWithError(redirectUri, request.State, "invalid_request", "PKCE with S256 is required");
        }

        if (!IsThisResource(request.Resource))
        {
            return RedirectWithError(redirectUri, request.State, "invalid_target");
        }

        var requestId = authorizationStore.SavePending(
            new PendingAuthorization(client.ClientId, client.ClientName, redirectUri, request.CodeChallenge!, request.State));

        return new AuthorizeOutcome.ShowSignIn(requestId, client.ClientName, new Uri(redirectUri).Host);
    }

    public async Task<AuthorizeOutcome> CompleteGoogleSignIn(string? requestId, string? credential)
    {
        var pending = authorizationStore.TakePending(requestId);
        if (pending is null)
        {
            return new AuthorizeOutcome.ShowError("This sign-in request has expired. Start again from your MCP client.");
        }

        User user;
        try
        {
            user = await authService.AuthenticateGoogleUserAsync(credential ?? string.Empty);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectWithError(pending.RedirectUri, pending.State, "access_denied");
        }

        var code = authorizationStore.IssueCode(pending, user.Id);
        logger.LogOperationSuccess("McpAuthorize", context: new { UserId = user.Id, pending.ClientName });

        return new AuthorizeOutcome.RedirectTo(BuildRedirect(pending.RedirectUri, new Dictionary<string, string?>
        {
            ["code"] = code,
            ["state"] = pending.State,
            ["iss"] = Options.Issuer
        }));
    }

    public async Task<OAuthResult<TokenResponse>> ExchangeToken(TokenRequest request)
    {
        var client = clientRegistrationService.GetClient(request.ClientId);
        if (client is null)
        {
            return OAuthResult<TokenResponse>.Fail("invalid_client");
        }

        if (!IsThisResource(request.Resource))
        {
            return OAuthResult<TokenResponse>.Fail("invalid_target");
        }

        return request.GrantType switch
        {
            "authorization_code" => await ExchangeCode(client, request),
            "refresh_token" => await ExchangeRefreshToken(request),
            _ => OAuthResult<TokenResponse>.Fail("unsupported_grant_type")
        };
    }

    private async Task<OAuthResult<TokenResponse>> ExchangeCode(RegisteredClient client, TokenRequest request)
    {
        var grant = authorizationStore.RedeemCode(request.Code);
        if (grant is null
            || grant.ClientId != client.ClientId
            || grant.RedirectUri != request.RedirectUri
            || !Pkce.VerifyS256(request.CodeVerifier, grant.CodeChallenge))
        {
            return OAuthResult<TokenResponse>.Fail("invalid_grant");
        }

        var user = await userRepository.GetUserAsync(grant.UserId);
        return user is null
            ? OAuthResult<TokenResponse>.Fail("invalid_grant")
            : OAuthResult<TokenResponse>.Ok(await IssueTokens(user));
    }

    // TODO(mcp): bind refresh tokens to client_id and keep them out of REST login's revoke-all (needs a RefreshToken column).
    private async Task<OAuthResult<TokenResponse>> ExchangeRefreshToken(TokenRequest request)
    {
        var stored = request.RefreshToken is null ? null : await refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        if (stored is null || !stored.IsActive)
        {
            return OAuthResult<TokenResponse>.Fail("invalid_grant");
        }

        stored.IsRevoked = true;
        await refreshTokenRepository.UpdateAsync(stored);

        return OAuthResult<TokenResponse>.Ok(await IssueTokens(stored.User));
    }

    private async Task<TokenResponse> IssueTokens(User user)
    {
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
        await refreshTokenRepository.AddAsync(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(Options.RefreshTokenDays)
        });

        return new TokenResponse
        {
            AccessToken = jwtTokenGenerator.GenerateToken(user, Options.Issuer, Options.Resource, Options.AccessTokenMinutes),
            ExpiresIn = Options.AccessTokenMinutes * 60,
            RefreshToken = refreshToken
        };
    }

    private bool IsThisResource(string? resource)
        => resource is null || string.Equals(resource.TrimEnd('/'), Options.Resource, StringComparison.Ordinal);

    private AuthorizeOutcome RedirectWithError(string redirectUri, string? state, string error, string? description = null)
        => new AuthorizeOutcome.RedirectTo(BuildRedirect(redirectUri, new Dictionary<string, string?>
        {
            ["error"] = error,
            ["error_description"] = description,
            ["state"] = state,
            ["iss"] = Options.Issuer
        }));

    private static string BuildRedirect(string redirectUri, Dictionary<string, string?> parameters)
        => QueryHelpers.AddQueryString(redirectUri, parameters.Where(p => p.Value is not null));
}
