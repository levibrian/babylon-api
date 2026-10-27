using Babylon.Alfred.Mcp.Models;
using Babylon.Alfred.Mcp.Models.Requests;
using Babylon.Alfred.Mcp.Models.Responses;
using Babylon.Alfred.Mcp.Services;
using Babylon.Alfred.Api.Shared.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Mcp.Controllers;

// OAuth endpoints return RFC 6749/7591/8414 shapes, not the ApiResponse envelope.
[ApiExplorerSettings(IgnoreApi = true)]
public class OAuthController(IMcpOAuthService oauthService, IClientRegistrationService clientRegistrationService, IConfiguration configuration)
    : BabylonControllerBase
{
    [HttpGet("/.well-known/oauth-authorization-server")]
    public IActionResult Metadata() => Ok(oauthService.GetMetadata());

    [HttpPost("/register")]
    public IActionResult Register([FromBody] ClientRegistrationRequest request)
    {
        var result = clientRegistrationService.Register(request);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : BadRequest(new OAuthErrorResponse(result.Error!, result.ErrorDescription));
    }

    [HttpGet("/authorize")]
    public IActionResult Authorize([FromQuery] AuthorizeRequest request) => Render(oauthService.Authorize(request));

    [HttpPost("/authorize/google")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> AuthorizeGoogle([FromForm(Name = "request_id")] string? requestId, [FromForm(Name = "credential")] string? credential)
        => Render(await oauthService.CompleteGoogleSignIn(requestId, credential));

    [HttpPost("/authorize/consent")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> AuthorizeConsent([FromForm(Name = "request_id")] string? requestId, [FromForm(Name = "decision")] string? decision)
        => Render(await oauthService.CompleteConsent(requestId, decision));

    [HttpPost("/token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] TokenRequest request)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";

        var result = await oauthService.ExchangeToken(request);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var error = new OAuthErrorResponse(result.Error!, result.ErrorDescription);
        return result.Error == "invalid_client" ? Unauthorized(error) : BadRequest(error);
    }

    private IActionResult Render(AuthorizeOutcome outcome)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
        Response.Headers["Referrer-Policy"] = "no-referrer-when-downgrade";

        return outcome switch
        {
            AuthorizeOutcome.RedirectTo redirect => Redirect(redirect.Url),
            AuthorizeOutcome.ShowSignIn signIn => Html(AuthorizePage.SignIn(
                configuration["Authentication:Google:ClientId"] ?? string.Empty,
                signIn.RequestId,
                signIn.ClientName,
                signIn.RedirectHost)),
            AuthorizeOutcome.ShowConsent consent => Html(AuthorizePage.Consent(
                consent.ClientName,
                consent.UserEmail,
                consent.RedirectHost,
                consent.RequestId)),
            AuthorizeOutcome.ShowError showError => Html(AuthorizePage.Error(showError.Message), StatusCodes.Status400BadRequest),
            _ => throw new InvalidOperationException("Unknown authorize outcome")
        };
    }

    private ContentResult Html(string content, int statusCode = StatusCodes.Status200OK)
        => new() { Content = content, ContentType = "text/html; charset=utf-8", StatusCode = statusCode };
}
