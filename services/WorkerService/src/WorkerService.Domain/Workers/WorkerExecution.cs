using CleanArchitecture.BuildingBlocks;

namespace WorkerService.Domain.Workers;

public sealed class WorkerExecution : Entity
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public string JobType { get; private set; } = string.Empty;
    public string WorkerId { get; private set; } = string.Empty;
    public int AttemptNumber { get; private set; }
    public ExecutionStatus Status { get; private set; }
    public string? ResultPayload { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private WorkerExecution() { }

    public static WorkerExecution Create(
        Guid jobId,
        string jobType,
        string workerId,
        int attemptNumber,
        DateTimeOffset now)
    {
        return new WorkerExecution
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            JobType = jobType,
            WorkerId = workerId,
            AttemptNumber = attemptNumber,
            Status = ExecutionStatus.Running,
            StartedAt = now
        };
    }

    public void MarkCompleted(string? resultPayload, DateTimeOffset now)
    {
        Status = ExecutionStatus.Completed;
        ResultPayload = resultPayload;
        CompletedAt = now;
    }

    public void MarkFailed(string errorMessage, DateTimeOffset now)
    {
        Status = ExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = now;
    }
}
