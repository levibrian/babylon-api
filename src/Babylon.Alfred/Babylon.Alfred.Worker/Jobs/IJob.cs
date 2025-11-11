using Quartz;

namespace Babylon.Alfred.Worker.Jobs;

/// <summary>
/// Base interface for all jobs in the worker application.
/// </summary>
public interface IJobBase : IJob
{
    /// <summary>
    /// Gets the cron expression for scheduling this job.
    /// </summary>
    string CronExpression { get; }
}

