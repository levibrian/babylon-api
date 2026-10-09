using Babylon.Alfred.Api.Features.Mcp.Models;
using Babylon.Alfred.Api.Features.Mcp.Models.Requests;
using Babylon.Alfred.Api.Features.Mcp.Models.Responses;

namespace Babylon.Alfred.Api.Features.Mcp.Services;

public interface IMcpOAuthService
{
    AuthorizationServerMetadataResponse GetMetadata();
    AuthorizeOutcome Authorize(AuthorizeRequest request);
    Task<AuthorizeOutcome> CompleteGoogleSignIn(string? requestId, string? credential);
    Task<OAuthResult<TokenResponse>> ExchangeToken(TokenRequest request);
}
