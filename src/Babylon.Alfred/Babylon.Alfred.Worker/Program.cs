using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Repositories;
using Babylon.Alfred.Worker.Extensions;
using Babylon.Alfred.Worker.Jobs;
using Babylon.Alfred.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Serilog;

// Configure Serilog before creating builder
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

// Serilog is already configured statically above
// The static Log.Logger will be used by the host

try
{
    Log.Information("Starting Babylon.Alfred.Worker application");

    // Configure database
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    // Log connection string (without password) for debugging
    var maskedConnectionString = connectionString.Contains("Password=")
        ? connectionString.Substring(0, connectionString.IndexOf("Password=") + 9) + "***"
        : connectionString;
    Log.Information("Worker connecting to database: {ConnectionString}", maskedConnectionString);

    builder.Services.AddDbContext<BabylonDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            // Enable retry on failure for transient errors
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

    // Register repositories
    builder.Services.AddScoped<IAllocationStrategyRepository, AllocationStrategyRepository>();
    builder.Services.AddScoped<IMarketPriceRepository, MarketPriceRepository>();

    // Register services
    builder.Services.ConfigureYahooClient();
    builder.Services.AddScoped<YahooFinanceService>();
    builder.Services.AddScoped<PriceFetchingService>();

    // Register jobs
    builder.Services.AddScoped<PriceFetchingJob>();

    // Configure Quartz
    builder.Services.AddQuartz(q =>
    {
        // Register job
        var priceFetchingJobKey = new JobKey("PriceFetchingJob");
        q.AddJob<PriceFetchingJob>(opts => opts.WithIdentity(priceFetchingJobKey));

        // Schedule job with cron expression (every hour at minute 0)
        // Hourly is sufficient for portfolio valuation - we only need EOD prices
        // Reference: https://github.com/Scarvy/yahoo-finance-api-collection
        q.AddTrigger(opts => opts
            .ForJob(priceFetchingJobKey)
            .WithIdentity("PriceFetchingJob-trigger")
            .WithCronSchedule("0 9-22 * * 1-5")); // Every day at 10 PM UTC (after US/EU markets close)
    });

    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    // Configure Serilog on the host builder
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    var host = builder.Build();

    Log.Information("Babylon.Alfred.Worker application started successfully");

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

