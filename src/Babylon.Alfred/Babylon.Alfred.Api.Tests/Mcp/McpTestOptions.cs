using Babylon.Alfred.Mcp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Babylon.Alfred.Api.Tests.Mcp;

internal static class McpTestOptions
{
    public const string SecretKey = "test-secret-key-for-mcp-tests-0123456789abcdef";
    public const string ClaudeCallback = "https://claude.ai/api/mcp/auth_callback";

    public static IOptions<McpOptions> Create(int codeSeconds = 60) => Options.Create(new McpOptions
    {
        BaseUrl = "https://babylon.test",
        AllowedRedirectUris = [ClaudeCallback, "http://localhost/callback"],
        AuthorizationCodeSeconds = codeSeconds
    });

    public static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:SecretKey"] = SecretKey,
            ["Authentication:Jwt:Issuer"] = "BabylonAlfredApi",
            ["Authentication:Jwt:Audience"] = "BabylonAlfredClient",
            ["Authentication:Jwt:ExpirationMinutes"] = "60"
        })
        .Build();
}
