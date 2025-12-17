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
    builder.Services.AddScoped<IPortfolioSnapshotRepository, PortfolioSnapshotRepository>();
    builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
    builder.Services.AddScoped<ISecurityRepository, SecurityRepository>();

    // Register services
    builder.Services.ConfigureYahooClient();
    builder.Services.AddScoped<YahooFinanceService>();
    builder.Services.AddScoped<PriceFetchingService>();
    builder.Services.AddScoped<PortfolioSnapshotService>();

    // Register jobs
    builder.Services.AddScoped<PriceFetchingJob>();
    builder.Services.AddScoped<PortfolioSnapshotJob>();

    // Configure Quartz scheduled jobs
    const string priceFetchingCron = "0 0 9-22 ? * MON-FRI"; // Every hour from 9 AM to 10 PM UTC, weekdays only
    const string portfolioSnapshotCron = "* * * * * ?"; // Every day at 11 PM UTC

    Log.Information("=== Job Configuration ===");
    Log.Information("PriceFetchingJob Schedule: {CronExpression}", priceFetchingCron);
    Log.Information("  → Runs every hour on the hour");
    Log.Information("  → Active hours: 9:00 AM - 10:00 PM UTC");
    Log.Information("  → Active days: Monday through Friday (market days)");
    Log.Information("  → Purpose: Fetch latest market prices from Yahoo Finance");
    Log.Information("");
    Log.Information("PortfolioSnapshotJob Schedule: {CronExpression}", portfolioSnapshotCron);
    Log.Information("  → Runs once daily at 11:00 PM UTC");
    Log.Information("  → Purpose: Save daily portfolio value, P&L, and P&L % for historical tracking");
    Log.Information("=========================");

    builder.Services.AddQuartz(q =>
    {
        var priceFetchingJobKey = new JobKey("PriceFetchingJob");
        q.AddJob<PriceFetchingJob>(opts => opts.WithIdentity(priceFetchingJobKey));

        q.AddTrigger(opts => opts
            .ForJob(priceFetchingJobKey)
            .WithIdentity("PriceFetchingJob-trigger")
            .WithCronSchedule(priceFetchingCron));

        var portfolioSnapshotJobKey = new JobKey("PortfolioSnapshotJob");
        q.AddJob<PortfolioSnapshotJob>(opts => opts.WithIdentity(portfolioSnapshotJobKey));

        q.AddTrigger(opts => opts
            .ForJob(portfolioSnapshotJobKey)
            .WithIdentity("PortfolioSnapshotJob-trigger")
            .WithCronSchedule(portfolioSnapshotCron));
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

