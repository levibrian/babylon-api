using System.Collections.Concurrent;
using System.Security.Cryptography;
using Babylon.Alfred.Api.Features.Mcp.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Babylon.Alfred.Api.Features.Mcp.Services;

// In-memory and single-instance by design: entries live for seconds to minutes.
// A restart only aborts sign-ins that are in flight.
public class AuthorizationStore(IOptions<McpOptions> options)
{
    private readonly ConcurrentDictionary<string, (PendingAuthorization Pending, DateTime ExpiresAt)> pending = new();
    private readonly ConcurrentDictionary<string, AuthorizationGrant> codes = new();

    public string SavePending(PendingAuthorization authorization)
    {
        Prune();
        var id = NewKey();
        pending[id] = (authorization, DateTime.UtcNow.AddMinutes(options.Value.PendingAuthorizationMinutes));
        return id;
    }

    public PendingAuthorization? TakePending(string? requestId)
    {
        if (requestId is null || !pending.TryRemove(requestId, out var entry) || entry.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        return entry.Pending;
    }

    public string IssueCode(PendingAuthorization authorization, Guid userId)
    {
        Prune();
        var code = NewKey();
        codes[code] = new AuthorizationGrant(
            authorization.ClientId,
            authorization.RedirectUri,
            authorization.CodeChallenge,
            userId,
            DateTime.UtcNow.AddSeconds(options.Value.AuthorizationCodeSeconds));
        return code;
    }

    public AuthorizationGrant? RedeemCode(string? code)
    {
        if (code is null || !codes.TryRemove(code, out var grant) || grant.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        return grant;
    }

    private void Prune()
    {
        var now = DateTime.UtcNow;
        foreach (var (key, value) in pending)
        {
            if (value.ExpiresAt <= now)
            {
                pending.TryRemove(key, out _);
            }
        }

        foreach (var (key, value) in codes)
        {
            if (value.ExpiresAt <= now)
            {
                codes.TryRemove(key, out _);
            }
        }
    }

    private static string NewKey() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
}
