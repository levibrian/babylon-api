namespace Babylon.Alfred.Api.Features.Mcp.Models;

public record RegisteredClient(string ClientId, string ClientName, IReadOnlyList<string> RedirectUris, long IssuedAt);
