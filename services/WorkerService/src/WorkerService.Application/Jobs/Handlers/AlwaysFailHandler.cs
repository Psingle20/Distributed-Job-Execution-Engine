using WorkerService.Application.Abstractions;

namespace WorkerService.Application.Jobs.Handlers;

public sealed class AlwaysFailHandler : IJobHandler
{
    public string JobType => "always-fail";

    public Task<string?> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            $"AlwaysFailHandler: deliberate failure for job {context.JobId} (attempt {context.AttemptNumber})");
    }
}
