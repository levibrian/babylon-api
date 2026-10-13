namespace Babylon.Alfred.Mcp.Models;

public record OAuthResult<T>(T? Value, string? Error = null, string? ErrorDescription = null)
{
    public bool IsSuccess => Error is null;

    public static OAuthResult<T> Ok(T value) => new(value);
    public static OAuthResult<T> Fail(string error, string? description = null) => new(default, error, description);
}
