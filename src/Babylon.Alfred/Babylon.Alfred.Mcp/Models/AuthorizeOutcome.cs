namespace Babylon.Alfred.Mcp.Models;

public abstract record AuthorizeOutcome
{
    public sealed record ShowError(string Message) : AuthorizeOutcome;

    public sealed record ShowSignIn(string RequestId, string ClientName, string RedirectHost) : AuthorizeOutcome;

    public sealed record RedirectTo(string Url) : AuthorizeOutcome;
}
