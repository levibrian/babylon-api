using Babylon.Alfred.Worker.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Babylon.Alfred.Worker.Jobs;

/// <summary>
/// Job that creates daily portfolio snapshots for all users.
/// Runs once per day after market close to capture portfolio performance metrics.
/// </summary>
[DisallowConcurrentExecution]
public class PortfolioSnapshotJob(
    PortfolioSnapshotService portfolioSnapshotService,
    ILogger<PortfolioSnapshotJob> logger)
    : IJobBase
{
    public string CronExpression => "0 0 23 * * ?"; // Every day at 11 PM UTC

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

