using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Babylon.Alfred.Api.Shared.Data;

public class BabylonDbContextFactory : IDesignTimeDbContextFactory<BabylonDbContext>
{
    public BabylonDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration or environment variables.");

        var optionsBuilder = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseNpgsql(connectionString);

        return new BabylonDbContext(optionsBuilder.Options);
    }
}

