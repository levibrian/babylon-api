namespace Babylon.Alfred.Mcp.Services;

public static class RedirectUriValidator
{
    public static bool IsAllowed(string? redirectUri, IEnumerable<string> allowlist)
        => redirectUri is not null && allowlist.Any(allowed => Matches(redirectUri, allowed));

    // Exact match, except loopback redirects (RFC 8252 7.3) which may use any port.
    public static bool Matches(string redirectUri, string allowed)
    {
        if (redirectUri.Contains('#'))
        {
            return false;
        }

        if (string.Equals(redirectUri, allowed, StringComparison.Ordinal))
        {
            return true;
        }

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri)
            || !Uri.TryCreate(allowed, UriKind.Absolute, out var allowedUri))
        {
            return false;
        }

        return IsLoopback(uri)
            && IsLoopback(allowedUri)
            && string.Equals(uri.Host, allowedUri.Host, StringComparison.OrdinalIgnoreCase)
            && string.Equals(uri.AbsolutePath, allowedUri.AbsolutePath, StringComparison.Ordinal)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(allowedUri.Query);
    }

    private static bool IsLoopback(Uri uri)
        => uri.Scheme == Uri.UriSchemeHttp
           && string.IsNullOrEmpty(uri.UserInfo)
           && uri.Host is "localhost" or "127.0.0.1";
}
