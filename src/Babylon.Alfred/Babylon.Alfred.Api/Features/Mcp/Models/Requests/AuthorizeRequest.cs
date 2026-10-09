using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Mcp.Models.Requests;

public class AuthorizeRequest
{
    [FromQuery(Name = "response_type")]
    public string? ResponseType { get; set; }

    [FromQuery(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromQuery(Name = "code_challenge")]
    public string? CodeChallenge { get; set; }

    [FromQuery(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }

    [FromQuery(Name = "state")]
    public string? State { get; set; }

    [FromQuery(Name = "resource")]
    public string? Resource { get; set; }
}
