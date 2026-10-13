using Babylon.Alfred.Mcp.Models;
using Babylon.Alfred.Mcp.Models.Requests;
using Babylon.Alfred.Mcp.Models.Responses;

namespace Babylon.Alfred.Mcp.Services;

public interface IClientRegistrationService
{
    OAuthResult<ClientRegistrationResponse> Register(ClientRegistrationRequest request);
    RegisteredClient? GetClient(string? clientId);
}
