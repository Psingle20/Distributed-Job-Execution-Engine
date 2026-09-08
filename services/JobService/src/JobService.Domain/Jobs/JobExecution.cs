namespace JobService.Domain.Jobs;

public sealed class JobExecution
{
    private JobExecution() { }

    public Guid Id { get; private set; }

    public Guid JobId { get; private set; }

    public string WorkerId { get; private set; } = string.Empty;

    public int AttemptNumber { get; private set; }

    public ExecutionStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? Error { get; private set; }

    public static JobExecution Create(Guid jobId, string workerId, int attemptNumber, DateTimeOffset startedAt)
    {
        return new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            WorkerId = workerId,
            AttemptNumber = attemptNumber,
            Status = ExecutionStatus.Running,
            StartedAt = startedAt
        };
    }

    public void MarkCompleted(DateTimeOffset completedAt)
    {
        Status = ExecutionStatus.Completed;
        CompletedAt = completedAt;
    }

    public void MarkFailed(DateTimeOffset failedAt, string error)
    {
        Status = ExecutionStatus.Failed;
        CompletedAt = failedAt;
        Error = error;
    }
}
