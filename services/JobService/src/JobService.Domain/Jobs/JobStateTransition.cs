namespace JobService.Domain.Jobs;

public sealed class JobStateTransition
{
    private JobStateTransition() { }

    public Guid Id { get; private set; }

    public Guid JobId { get; private set; }

    public JobState? FromState { get; private set; }

    public JobState ToState { get; private set; }

    public string? Reason { get; private set; }

    public string? WorkerId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static JobStateTransition Create(
        Guid jobId,
        JobState? fromState,
        JobState toState,
        DateTimeOffset occurredAt,
        string? reason = null,
        string? workerId = null)
    {
        return new JobStateTransition
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            FromState = fromState,
            ToState = toState,
            Reason = reason,
            WorkerId = workerId,
            OccurredAt = occurredAt
        };
    }
}
