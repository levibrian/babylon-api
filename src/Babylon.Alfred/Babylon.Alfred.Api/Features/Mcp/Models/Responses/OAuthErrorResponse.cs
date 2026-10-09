using Newtonsoft.Json;

namespace Babylon.Alfred.Api.Features.Mcp.Models.Responses;

public class OAuthErrorResponse(string error, string? errorDescription)
{
    [JsonProperty("error")]
    public string Error { get; } = error;

    [JsonProperty("error_description", NullValueHandling = NullValueHandling.Ignore)]
    public string? ErrorDescription { get; } = errorDescription;
}
