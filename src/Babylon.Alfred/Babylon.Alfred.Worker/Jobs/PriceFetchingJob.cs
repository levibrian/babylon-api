using Babylon.Alfred.Worker.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Babylon.Alfred.Worker.Jobs;

[DisallowConcurrentExecution]
public class PriceFetchingJob(
    PriceFetchingService priceFetchingService,
    ILogger<PriceFetchingJob> logger)
    : IJobBase
{
    /// <summary>
    /// Every hour from 9 AM to 10 PM UTC, weekdays only.
    /// </summary>
    public const string Cron = "0 0 9-22 ? * MON-FRI";
    
    public string CronExpression => Cron;

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("PriceFetchingJob started at {Timestamp}", DateTime.UtcNow);

        try
        {
            await priceFetchingService.ExecuteAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing PriceFetchingJob");
            throw new JobExecutionException(ex, refireImmediately: false);
        }

        logger.LogInformation("PriceFetchingJob completed at {Timestamp}", DateTime.UtcNow);
    }
}

