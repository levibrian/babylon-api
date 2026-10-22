using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Babylon.Alfred.Api.IntegrationTests;

/// <summary>
/// Boots the real Api in-process (WebApplicationFactory) against an ephemeral Postgres
/// container (Testcontainers) — real migrations, real constraints, no mocks. The container
/// is thrown away when the test run ends; it never touches the RDS dev database.
///
/// Config is injected via real process environment variables, set before the host is built
/// (not via ConfigureWebHost/ConfigureAppConfiguration) — Program.cs reads
/// ConnectionStrings:DefaultConnection and Authentication:Jwt:SecretKey BEFORE builder.Build(),
/// so a ConfigureAppConfiguration override (which only materializes as part of Build()) arrives
/// too late and those reads silently fall back to appsettings' real RDS connection string.
/// </summary>
public class BabylonApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecretKey = "integration-test-signing-key-at-least-32-characters-long";
    public const string JwtIssuer = "babylon-integration-tests";
    public const string JwtAudience = "babylon-integration-tests";

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Authentication__Jwt__SecretKey", JwtSecretKey);
        Environment.SetEnvironmentVariable("Authentication__Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Authentication__Jwt__Audience", JwtAudience);
        Environment.SetEnvironmentVariable("Authentication__Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("Authentication__Jwt__RefreshTokenExpirationDays", "7");

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BabylonDbContext>();
        await context.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => postgres.DisposeAsync().AsTask();

    /// <summary>Inserts a Security directly, bypassing the live Yahoo Finance lookup Create() would trigger.</summary>
    public async Task<Security> SeedSecurityAsync(string ticker, string name, SecurityType type = SecurityType.Stock, string? isin = null)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BabylonDbContext>();

        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            SecurityName = name,
            SecurityType = type,
            Isin = isin
        };

        context.Securities.Add(security);
        await context.SaveChangesAsync();
        return security;
    }
}
