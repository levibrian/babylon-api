namespace Babylon.Alfred.Mcp.Models;

public class McpOptions
{
    public const string SectionName = "Mcp";

    public string BaseUrl { get; set; } = string.Empty;
    public string[] AllowedRedirectUris { get; set; } = [];
    public int AccessTokenMinutes { get; set; } = 60;
    public int AuthorizationCodeSeconds { get; set; } = 60;
    public int PendingAuthorizationMinutes { get; set; } = 10;
    public int RefreshTokenDays { get; set; } = 30;

    public string Issuer => BaseUrl.TrimEnd('/');
    public string Resource => $"{Issuer}/mcp";
    public string ResourceMetadataUrl => $"{Issuer}/.well-known/oauth-protected-resource/mcp";
}
