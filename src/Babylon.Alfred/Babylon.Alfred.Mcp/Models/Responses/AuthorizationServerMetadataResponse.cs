using Newtonsoft.Json;

namespace Babylon.Alfred.Mcp.Models.Responses;

public class AuthorizationServerMetadataResponse
{
    [JsonProperty("issuer")]
    public string Issuer { get; set; } = string.Empty;

    [JsonProperty("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    [JsonProperty("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    [JsonProperty("registration_endpoint")]
    public string RegistrationEndpoint { get; set; } = string.Empty;

    [JsonProperty("response_types_supported")]
    public string[] ResponseTypesSupported { get; set; } = ["code"];

    [JsonProperty("grant_types_supported")]
    public string[] GrantTypesSupported { get; set; } = ["authorization_code", "refresh_token"];

    [JsonProperty("code_challenge_methods_supported")]
    public string[] CodeChallengeMethodsSupported { get; set; } = ["S256"];

    [JsonProperty("token_endpoint_auth_methods_supported")]
    public string[] TokenEndpointAuthMethodsSupported { get; set; } = ["none"];

    [JsonProperty("authorization_response_iss_parameter_supported")]
    public bool AuthorizationResponseIssParameterSupported { get; set; } = true;
}
