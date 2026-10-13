using Babylon.Alfred.Mcp.Models;
using Babylon.Alfred.Mcp.Models.Requests;
using Babylon.Alfred.Mcp.Models.Responses;

namespace Babylon.Alfred.Mcp.Services;

public interface IMcpOAuthService
{
    AuthorizationServerMetadataResponse GetMetadata();
    AuthorizeOutcome Authorize(AuthorizeRequest request);
    Task<AuthorizeOutcome> CompleteGoogleSignIn(string? requestId, string? credential);
    Task<OAuthResult<TokenResponse>> ExchangeToken(TokenRequest request);
}
