using Newtonsoft.Json;

namespace Babylon.Alfred.Mcp.Models.Requests;

public class ClientRegistrationRequest
{
    [JsonProperty("redirect_uris")]
    public List<string>? RedirectUris { get; set; }

    [JsonProperty("client_name")]
    public string? ClientName { get; set; }

    [JsonProperty("grant_types")]
    public List<string>? GrantTypes { get; set; }

    [JsonProperty("response_types")]
    public List<string>? ResponseTypes { get; set; }

    [JsonProperty("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; set; }
}
