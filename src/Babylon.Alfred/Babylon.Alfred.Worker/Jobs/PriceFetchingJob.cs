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
    public string CronExpression => "* * * * *"; // Every minute

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

