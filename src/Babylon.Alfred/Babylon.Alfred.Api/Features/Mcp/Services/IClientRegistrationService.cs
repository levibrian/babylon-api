using Babylon.Alfred.Api.Features.Mcp.Models;
using Babylon.Alfred.Api.Features.Mcp.Models.Requests;
using Babylon.Alfred.Api.Features.Mcp.Models.Responses;

namespace Babylon.Alfred.Api.Features.Mcp.Services;

public interface IClientRegistrationService
{
    OAuthResult<ClientRegistrationResponse> Register(ClientRegistrationRequest request);
    RegisteredClient? GetClient(string? clientId);
}
