using Newtonsoft.Json;

namespace Babylon.Alfred.Mcp.Models.Responses;

public class ClientRegistrationResponse
{
    [JsonProperty("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonProperty("client_id_issued_at")]
    public long ClientIdIssuedAt { get; set; }

    [JsonProperty("client_name")]
    public string ClientName { get; set; } = string.Empty;

    [JsonProperty("redirect_uris")]
    public IReadOnlyList<string> RedirectUris { get; set; } = [];

    [JsonProperty("grant_types")]
    public string[] GrantTypes { get; set; } = ["authorization_code", "refresh_token"];

    [JsonProperty("response_types")]
    public string[] ResponseTypes { get; set; } = ["code"];

    [JsonProperty("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; set; } = "none";
}
