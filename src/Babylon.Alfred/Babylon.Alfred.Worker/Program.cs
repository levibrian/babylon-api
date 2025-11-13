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

var builder = Host.CreateApplicationBuilder(args);

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Log connection string (without password) for debugging
var tempLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var tempLogger = tempLoggerFactory.CreateLogger<Program>();
var maskedConnectionString = connectionString.Contains("Password=") 
    ? connectionString.Substring(0, connectionString.IndexOf("Password=") + 9) + "***" 
    : connectionString;
tempLogger.LogInformation("Worker connecting to database: {ConnectionString}", maskedConnectionString);

builder.Services.AddDbContext<BabylonDbContext>(options =>
    options.UseNpgsql(connectionString));

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

    // Schedule job with cron expression (every minute)
    q.AddTrigger(opts => opts
        .ForJob(priceFetchingJobKey)
        .WithIdentity("PriceFetchingJob-trigger")
        .WithCronSchedule("0 * * * * ?")); // Every minute at second 0
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var host = builder.Build();
host.Run();

