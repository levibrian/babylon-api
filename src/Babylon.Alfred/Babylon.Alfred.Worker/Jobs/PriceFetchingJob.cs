using Babylon.Alfred.Worker.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Babylon.Alfred.Worker.Jobs;

[DisallowConcurrentExecution]
public class PriceFetchingJob : IJobBase
{
    public string CronExpression => "* * * * *"; // Every minute

    private readonly PriceFetchingService _priceFetchingService;
    private readonly ILogger<PriceFetchingJob> _logger;

    public PriceFetchingJob(
        PriceFetchingService priceFetchingService,
        ILogger<PriceFetchingJob> logger)
    {
        _priceFetchingService = priceFetchingService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("PriceFetchingJob started at {Timestamp}", DateTime.UtcNow);

        try
        {
            await _priceFetchingService.ExecuteAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing PriceFetchingJob");
            throw new JobExecutionException(ex, refireImmediately: false);
        }

        _logger.LogInformation("PriceFetchingJob completed at {Timestamp}", DateTime.UtcNow);
    }
}

