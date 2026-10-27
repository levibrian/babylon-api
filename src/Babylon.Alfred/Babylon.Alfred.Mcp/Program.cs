using System.Text;
using Babylon.Alfred.Api.Features.Startup.Extensions;
using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Mcp.Extensions;
using Babylon.Alfred.Mcp.Logging;
using McpExtensions = Babylon.Alfred.Mcp.Extensions.ServiceCollectionExtensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .Enrich.With<EndpointNameVersionEnricher>());

try
{
    Log.Information("Starting Babylon.Alfred.Mcp application");

    var secretKey = builder.Configuration["Authentication:Jwt:SecretKey"];
    if (string.IsNullOrEmpty(secretKey))
    {
        throw new InvalidOperationException("Authentication:Jwt:SecretKey is not configured.");
    }

    builder.Services
        .AddControllers()
        .AddNewtonsoftJson()
        .ConfigureApplicationPartManager(manager =>
        {
            var mcpAssembly = typeof(McpExtensions).Assembly;
            foreach (var part in manager.ApplicationParts.OfType<AssemblyPart>().Where(p => p.Assembly != mcpAssembly).ToList())
            {
                manager.ApplicationParts.Remove(part);
            }
        });

    builder.Services.AddDbContext<BabylonDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null)));

    builder.Services.RegisterFeatures();
    builder.Services.RegisterMcpServices(builder.Configuration);

    builder.Services
        .AddAuthentication(McpExtensions.BearerScheme)
        .AddMcpAuthentication(builder.Configuration, Encoding.UTF8.GetBytes(secretKey));

    var app = builder.Build();

    var forwardedHeadersOptions = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto };
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);

    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
    app.MapControllers();
    app.MapMcpFeature();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Babylon.Alfred.Mcp terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
