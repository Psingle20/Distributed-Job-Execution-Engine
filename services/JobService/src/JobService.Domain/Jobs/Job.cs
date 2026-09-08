using CleanArchitecture.BuildingBlocks;
using JobService.Domain.Jobs.Events;

namespace JobService.Domain.Jobs;

public sealed class Job : Entity
{
    private readonly List<JobStateTransition> _stateTransitions = [];
    private readonly List<JobExecution> _executions = [];

    private Job() { }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;

    public JobState State { get; private set; }

    public int AttemptCount { get; private set; }

    public int MaxAttempts { get; private set; }

    public string? WorkerId { get; private set; }

    public DateTimeOffset? LeaseUntil { get; private set; }

    public DateTimeOffset? LastHeartbeatAt { get; private set; }

    public DateTimeOffset? NextRunAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public string? LastError { get; private set; }

    public uint Version { get; init; }

    public IReadOnlyList<JobStateTransition> StateTransitions => _stateTransitions.AsReadOnly();

    public IReadOnlyList<JobExecution> Executions => _executions.AsReadOnly();

    public static Job Create(
        string type,
        string payloadJson,
        int maxAttempts,
        DateTimeOffset now)
    {
        Job job = new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            PayloadJson = payloadJson,
            State = JobState.Pending,
            AttemptCount = 0,
            MaxAttempts = maxAttempts,
            CreatedAt = now
        };

        job.RecordTransition(null, JobState.Pending, now, reason: "Job created");
        job.Raise(new JobCreatedDomainEvent(job.Id, job.Type, now));

        return job;
    }

    public Result Claim(string workerId, DateTimeOffset now, TimeSpan leaseDuration)
    {
        if (!CanBeClaimed(now))
        {
            return Result.Failure(JobErrors.NotClaimable);
        }

        if (AttemptCount >= MaxAttempts)
        {
            return Result.Failure(JobErrors.MaxAttemptsReached);
        }

        JobState previousState = State;
        TransitionTo(JobState.Running);
        WorkerId = workerId;
        LeaseUntil = now + leaseDuration;
        LastHeartbeatAt = now;
        StartedAt ??= now;
        NextRunAt = null;
        AttemptCount++;

        _executions.Add(JobExecution.Create(Id, workerId, AttemptCount, now));
        RecordTransition(previousState, JobState.Running, now, workerId: workerId, reason: "Claimed by worker");
        Raise(new JobClaimedDomainEvent(Id, workerId, AttemptCount));

        return Result.Success();
    }

    public Result Heartbeat(string workerId, DateTimeOffset now, TimeSpan leaseDuration)
    {
        if (State != JobState.Running)
        {
            return Result.Failure(JobErrors.NotRunning);
        }

        if (WorkerId != workerId)
        {
            return Result.Failure(JobErrors.NotLeaseOwner);
        }

        LeaseUntil = now + leaseDuration;
        LastHeartbeatAt = now;

        return Result.Success();
    }

    public Result Complete(string workerId, DateTimeOffset now)
    {
        if (State != JobState.Running)
        {
            return Result.Failure(JobErrors.NotRunning);
        }

        if (WorkerId != workerId)
        {
            return Result.Failure(JobErrors.NotLeaseOwner);
        }

        TransitionTo(JobState.Completed);
        CompletedAt = now;
        ClearLease();

        MarkCurrentExecution(e => e.MarkCompleted(now));
        RecordTransition(JobState.Running, JobState.Completed, now, workerId: workerId, reason: "Completed successfully");
        Raise(new JobCompletedDomainEvent(Id, workerId, now));

        return Result.Success();
    }

    public Result Fail(string workerId, DateTimeOffset now, string error)
    {
        if (State != JobState.Running)
        {
            return Result.Failure(JobErrors.NotRunning);
        }

        if (WorkerId != workerId)
        {
            return Result.Failure(JobErrors.NotLeaseOwner);
        }

        LastError = error;
        MarkCurrentExecution(e => e.MarkFailed(now, error));

        if (AttemptCount < MaxAttempts)
        {
            TimeSpan delay = JobRetryPolicy.CalculateDelay(AttemptCount);
            DateTimeOffset retryAt = now + delay;
            return ScheduleRetryInternal(workerId, now, retryAt, error);
        }

        TransitionTo(JobState.Failed);
        FailedAt = now;
        ClearLease();

        RecordTransition(JobState.Running, JobState.Failed, now, workerId: workerId, reason: $"Failed permanently: {error}");
        Raise(new JobFailedDomainEvent(Id, error, now));

        return Result.Success();
    }

    public Result ScheduleRetry(string workerId, DateTimeOffset now, DateTimeOffset retryAt, string error)
    {
        if (State != JobState.Running)
        {
            return Result.Failure(JobErrors.NotRunning);
        }

        if (WorkerId != workerId)
        {
            return Result.Failure(JobErrors.NotLeaseOwner);
        }

        LastError = error;
        MarkCurrentExecution(e => e.MarkFailed(now, error));

        return ScheduleRetryInternal(workerId, now, retryAt, error);
    }

    public Result Cancel(DateTimeOffset now)
    {
        if (JobStateMachine.IsTerminal(State))
        {
            return Result.Failure(JobErrors.AlreadyTerminal);
        }

        JobState previousState = State;
        TransitionTo(JobState.Cancelled);
        ClearLease();

        RecordTransition(previousState, JobState.Cancelled, now, reason: "Cancelled by user");
        Raise(new JobCancelledDomainEvent(Id, now));

        return Result.Success();
    }

    public Result MarkLeaseExpired(DateTimeOffset now, DateTimeOffset retryAt)
    {
        if (State != JobState.Running)
        {
            return Result.Failure(JobErrors.NotRunning);
        }

        string? previousWorker = WorkerId;

        MarkCurrentExecution(e => e.MarkFailed(now, "Lease expired"));

        if (AttemptCount < MaxAttempts)
        {
            TransitionTo(JobState.Retrying);
            NextRunAt = retryAt;
            LastError = "Lease expired";
            ClearLease();

            RecordTransition(JobState.Running, JobState.Retrying, now, workerId: previousWorker, reason: "Lease expired — scheduling retry");
            Raise(new JobLeaseExpiredDomainEvent(Id, previousWorker, now));
            Raise(new JobRetryScheduledDomainEvent(Id, AttemptCount, retryAt));
        }
        else
        {
            TransitionTo(JobState.Failed);
            FailedAt = now;
            LastError = "Lease expired — max attempts reached";
            ClearLease();

            RecordTransition(JobState.Running, JobState.Failed, now, workerId: previousWorker, reason: "Lease expired — max attempts reached");
            Raise(new JobLeaseExpiredDomainEvent(Id, previousWorker, now));
            Raise(new JobFailedDomainEvent(Id, LastError, now));
        }

        return Result.Success();
    }

    public Result RetryFromFailed(DateTimeOffset now)
    {
        if (State != JobState.Failed)
        {
            return Result.Failure(JobErrors.NotFailed);
        }

        TransitionTo(JobState.Retrying);
        NextRunAt = now;
        FailedAt = null;

        RecordTransition(JobState.Failed, JobState.Retrying, now, reason: "Manual retry requested");
        Raise(new JobRetryScheduledDomainEvent(Id, AttemptCount, now));

        return Result.Success();
    }

    public bool CanBeClaimed(DateTimeOffset now) =>
        State is JobState.Pending ||
        State == JobState.Retrying && (NextRunAt == null || NextRunAt <= now);

    private Result ScheduleRetryInternal(string workerId, DateTimeOffset now, DateTimeOffset retryAt, string error)
    {
        TransitionTo(JobState.Retrying);
        NextRunAt = retryAt;
        ClearLease();

        RecordTransition(JobState.Running, JobState.Retrying, now, workerId: workerId, reason: $"Retrying after error: {error}");
        Raise(new JobRetryScheduledDomainEvent(Id, AttemptCount, retryAt));

        return Result.Success();
    }

    private void TransitionTo(JobState newState)
    {
        if (!JobStateMachine.CanTransition(State, newState))
        {
            throw new InvalidOperationException(
                $"Invalid state transition from {State} to {newState}.");
        }

        State = newState;
    }

    private void ClearLease()
    {
        WorkerId = null;
        LeaseUntil = null;
        LastHeartbeatAt = null;
    }

    private void RecordTransition(
        JobState? fromState,
        JobState toState,
        DateTimeOffset occurredAt,
        string? reason = null,
        string? workerId = null)
    {
        _stateTransitions.Add(JobStateTransition.Create(Id, fromState, toState, occurredAt, reason, workerId));
    }

    private void MarkCurrentExecution(Action<JobExecution> action)
    {
        JobExecution? current = _executions.Find(
            e => e.AttemptNumber == AttemptCount && e.Status == ExecutionStatus.Running);

        if (current is not null)
        {
            action(current);
        }
    }
}
