namespace WorkerService.Application.Abstractions;

public interface IJobHandler
{
    string JobType { get; }
    Task<string?> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken);
}

public sealed record JobExecutionContext(
    Guid JobId,
    string JobType,
    string PayloadJson,
    int AttemptNumber,
    string WorkerId);
