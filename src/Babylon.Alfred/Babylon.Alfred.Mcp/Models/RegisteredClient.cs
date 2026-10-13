namespace Babylon.Alfred.Mcp.Models;

public record RegisteredClient(string ClientId, string ClientName, IReadOnlyList<string> RedirectUris, long IssuedAt);
