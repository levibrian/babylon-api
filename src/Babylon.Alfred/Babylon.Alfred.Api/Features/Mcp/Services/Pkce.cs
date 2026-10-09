using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Babylon.Alfred.Api.Features.Mcp.Services;

public static class Pkce
{
    public static bool IsValidChallenge(string? codeChallenge)
        => codeChallenge is { Length: 43 } && codeChallenge.All(IsBase64UrlChar);

    public static bool VerifyS256(string? codeVerifier, string codeChallenge)
    {
        if (codeVerifier is null || codeVerifier.Length is < 43 or > 128 || !codeVerifier.All(IsUnreserved))
        {
            return false;
        }

        var computed = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed),
            Encoding.ASCII.GetBytes(codeChallenge));
    }

    private static bool IsBase64UrlChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '_';

    private static bool IsUnreserved(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~';
}
