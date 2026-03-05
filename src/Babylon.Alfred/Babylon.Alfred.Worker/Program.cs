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
    builder.Services.AddScoped<ICashBalanceRepository, CashBalanceRepository>();

    // Register services
    builder.Services.ConfigureYahooClient();
    builder.Services.AddScoped<YahooFinanceService>();
    builder.Services.AddScoped<PriceFetchingService>();
    builder.Services.AddScoped<PortfolioSnapshotService>();
    builder.Services.AddScoped<RealizedPnlBackfillService>();

    // Register jobs
    builder.Services.AddScoped<PriceFetchingJob>();
    builder.Services.AddScoped<PortfolioSnapshotJob>();
    builder.Services.AddScoped<RealizedPnlBackfillJob>();


    builder.Services.AddQuartz(q =>
    {
        var priceFetchingJobKey = new JobKey(nameof(PriceFetchingJob));
        q.AddJob<PriceFetchingJob>(opts => opts.WithIdentity(priceFetchingJobKey));

        q.AddTrigger(opts => opts
            .ForJob(priceFetchingJobKey)
            .WithIdentity($"{nameof(PriceFetchingJob)}-trigger")
            .WithCronSchedule(PriceFetchingJob.Cron));

        var portfolioSnapshotJobKey = new JobKey(nameof(PortfolioSnapshotJob));
        q.AddJob<PortfolioSnapshotJob>(opts => opts.WithIdentity(portfolioSnapshotJobKey));

        q.AddTrigger(opts => opts
            .ForJob(portfolioSnapshotJobKey)
            .WithIdentity($"{nameof(PortfolioSnapshotJob)}-trigger")
            .WithCronSchedule(PortfolioSnapshotJob.Cron));

        var realizedPnlBackfillJobKey = new JobKey(nameof(RealizedPnlBackfillJob));
        q.AddJob<RealizedPnlBackfillJob>(opts => opts.WithIdentity(realizedPnlBackfillJobKey));

        q.AddTrigger(opts => opts
            .ForJob(realizedPnlBackfillJobKey)
            .WithIdentity($"{nameof(RealizedPnlBackfillJob)}-trigger")
            .WithCronSchedule(RealizedPnlBackfillJob.Cron));
    });

    // Log the scheduler summary after all jobs are registered
    Log.Information("Quartz scheduler configured — {JobCount} job(s) registered", 3);
    Log.Information("Job {Job} scheduled | Cron: {Cron} | Market price fetch, weekdays 09:00–22:00 UTC",
        nameof(PriceFetchingJob), PriceFetchingJob.Cron);
    Log.Information("Job {Job} scheduled | Cron: {Cron} | Portfolio snapshots, weekdays 09:15–22:15 UTC",
        nameof(PortfolioSnapshotJob), PortfolioSnapshotJob.Cron);
    Log.Information("Job {Job} scheduled | Cron: {Cron} | Sell PnL backfill, daily 03:00 UTC (idempotent)",
        nameof(RealizedPnlBackfillJob), RealizedPnlBackfillJob.Cron);

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

