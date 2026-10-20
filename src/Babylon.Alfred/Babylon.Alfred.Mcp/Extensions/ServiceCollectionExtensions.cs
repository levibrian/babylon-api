using Babylon.Alfred.Mcp.Models;
using Babylon.Alfred.Mcp.Services;
using Babylon.Alfred.Mcp.Tools;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.AspNetCore.Authentication;

namespace Babylon.Alfred.Mcp.Extensions;

public static class ServiceCollectionExtensions
{
    public const string BearerScheme = "McpBearer";
    public const string AuthorizationPolicy = "Mcp";

    public static void RegisterMcpServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<McpOptions>(configuration.GetSection(McpOptions.SectionName));
        services.AddSingleton<AuthorizationStore>();
        services.AddScoped<IClientRegistrationService, ClientRegistrationService>();
        services.AddScoped<IMcpOAuthService, McpOAuthService>();

        services.AddAuthorization(options => options.AddPolicy(AuthorizationPolicy, policy => policy
            .AddAuthenticationSchemes(BearerScheme)
            .RequireAuthenticatedUser()));

        var contextPath = Path.Combine(AppContext.BaseDirectory, "mcp-context.md");
        var serverInstructions = File.Exists(contextPath) ? File.ReadAllText(contextPath) : null;

        services.AddMcpServer(options => options.ServerInstructions = serverInstructions)
            .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
            .WithTools<WhoAmITool>()
            .WithTools<PortfolioTool>()
            .WithTools<TransactionsTool>()
            .WithTools<TargetsTool>()
            .WithTools<RebalancingTool>()
            .WithTools<ImportTool>();
    }

    // Separate bearer scheme: MCP tokens carry the /mcp resource as audience, so REST tokens
    // are rejected at /mcp and MCP tokens are rejected by the REST API (RFC 8707).
    public static AuthenticationBuilder AddMcpAuthentication(this AuthenticationBuilder builder, IConfiguration configuration, byte[] signingKey)
    {
        var options = configuration.GetSection(McpOptions.SectionName).Get<McpOptions>() ?? new McpOptions();

        return builder
            .AddJwtBearer(BearerScheme, jwt =>
            {
                jwt.ForwardChallenge = McpAuthenticationDefaults.AuthenticationScheme;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Resource,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddMcp(mcp =>
            {
                mcp.ResourceMetadataUri = new Uri(options.ResourceMetadataUrl);
                mcp.ResourceMetadata = new()
                {
                    Resource = options.Resource,
                    AuthorizationServers = { options.Issuer },
                    ResourceName = "Babylon"
                };
            });
    }

    public static void MapMcpFeature(this IEndpointRouteBuilder app)
    {
        app.MapMcp("/mcp").RequireAuthorization(AuthorizationPolicy);
    }
}
