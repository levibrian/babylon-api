using Babylon.Alfred.Worker.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Babylon.Alfred.Worker.Jobs;

/// <summary>
/// Scheduled job that backfills RealizedPnL and RealizedPnLPct on historical Sell transactions.
///
/// Runs daily at 3:00 AM UTC — outside market hours and outside the price-fetch window (9–22 UTC weekdays).
/// Idempotent: delegates to <see cref="RealizedPnlBackfillService"/> which only processes
/// Sell transactions where RealizedPnL IS NULL. Becomes a no-op once all rows are populated.
/// </summary>
[DisallowConcurrentExecution]
public class RealizedPnlBackfillJob(
    RealizedPnlBackfillService backfillService,
    ILogger<RealizedPnlBackfillJob> logger)
    : IJobBase
{
    /// <summary>
    /// Daily at 3:00 AM UTC, every day of the week.
    /// </summary>
    public const string Cron = "0 0 3 * * ?";

    public string CronExpression => Cron;

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("RealizedPnlBackfillJob started");

        try
        {
            await backfillService.ExecuteAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RealizedPnlBackfillJob failed — will not refire");
            throw new JobExecutionException(ex, refireImmediately: false);
        }

        logger.LogInformation("RealizedPnlBackfillJob completed");
    }
}
