extern alias McpAssembly;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Babylon.Alfred.Api.IntegrationTests.Mcp;

/// <summary>
/// Boots the real Mcp host in-process (WebApplicationFactory). Deliberately has no Postgres
/// container of its own — it reuses the connection string + JWT env vars that
/// <see cref="BabylonApiFactory"/> already set for the shared "Integration" collection. A second
/// container here previously raced the shared one for a host port across parallel xUnit
/// collections and intermittently broke both (CI incident 2026-10-27).
/// </summary>
internal class McpApiFactory : WebApplicationFactory<McpAssembly::Program>
{
    public McpApiFactory() => Environment.SetEnvironmentVariable("Mcp__BaseUrl", "https://babylon-mcp.test");
}
