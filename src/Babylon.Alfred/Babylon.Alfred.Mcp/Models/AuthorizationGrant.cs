namespace Babylon.Alfred.Mcp.Models;

public record PendingAuthorization(
    string ClientId,
    string ClientName,
    string RedirectUri,
    string CodeChallenge,
    string? State,
    Guid? UserId = null,
    string? UserEmail = null);

public record AuthorizationGrant(
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    Guid UserId,
    DateTime ExpiresAt);
