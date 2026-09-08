using CleanArchitecture.BuildingBlocks;

namespace WorkerService.Domain.Workers;

public static class WorkerErrors
{
    public static readonly Error ConcurrencyLimitReached = Error.Problem(
        "Worker.ConcurrencyLimitReached",
        "Worker has reached its maximum concurrent execution limit.");

    public static readonly Error ExecutionNotFound = Error.NotFound(
        "Worker.ExecutionNotFound",
        "The specified execution was not found.");

    public static readonly Error ClaimFailed = Error.Problem(
        "Worker.ClaimFailed",
        "Failed to claim job from JobService.");

    public static readonly Error AlreadyDraining = Error.Problem(
        "Worker.AlreadyDraining",
        "Worker is draining and not accepting new jobs.");
}
