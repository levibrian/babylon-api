using Babylon.Alfred.Worker.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Babylon.Alfred.Worker.Jobs;

/// <summary>
/// Job that creates hourly portfolio snapshots for all users.
/// Runs 15 minutes after each price fetch to capture portfolio performance metrics.
/// </summary>
[DisallowConcurrentExecution]
public class PortfolioSnapshotJob(
    PortfolioSnapshotService portfolioSnapshotService,
    ILogger<PortfolioSnapshotJob> logger)
    : IJobBase
{
    /// <summary>
    /// 15 minutes after each hour (after price fetch), 9 AM to 10 PM UTC, weekdays only.
    /// </summary>
    public const string Cron = "0 15 9-22 ? * MON-FRI";
    
    public string CronExpression => Cron;

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("PortfolioSnapshotJob started at {Timestamp}", DateTime.UtcNow);

        try
        {
            await portfolioSnapshotService.ExecuteAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing PortfolioSnapshotJob");
            throw new JobExecutionException(ex, refireImmediately: false);
        }

        logger.LogInformation("PortfolioSnapshotJob completed at {Timestamp}", DateTime.UtcNow);
    }
}

