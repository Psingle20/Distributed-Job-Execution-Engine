using CleanArchitecture.BuildingBlocks;

namespace JobService.Domain.Jobs;

public static class JobErrors
{
    public static Error NotFound(Guid jobId) =>
        Error.NotFound("Job.NotFound", $"Job '{jobId}' was not found.");

    public static readonly Error InvalidStateTransition =
        Error.Problem("Job.InvalidStateTransition", "The requested state transition is not allowed.");

    public static readonly Error NotClaimable =
        Error.Conflict("Job.NotClaimable", "The job cannot be claimed in its current state.");

    public static readonly Error MaxAttemptsReached =
        Error.Problem("Job.MaxAttemptsReached", "The job has reached its maximum number of attempts.");

    public static readonly Error NotLeaseOwner =
        Error.Conflict("Job.NotLeaseOwner", "Only the current lease owner can perform this operation.");

    public static readonly Error NotRunning =
        Error.Problem("Job.NotRunning", "The job is not in a running state.");

    public static readonly Error AlreadyTerminal =
        Error.Problem("Job.AlreadyTerminal", "The job is in a terminal state and cannot be modified.");

    public static readonly Error NotFailed =
        Error.Problem("Job.NotFailed", "Only failed jobs can be retried.");

    public static readonly Error StaleExecution =
        Error.Conflict("Job.StaleExecution", "The execution ID does not match the current execution.");
}
