using CleanArchitecture.BuildingBlocks;

namespace WorkerService.Application.Abstractions;

public sealed record ClaimResult(Guid ExecutionId, int AttemptNumber, DateTimeOffset LeaseExpiresAt);

public interface IJobServiceClient
{
    Task<Result<ClaimResult>> ClaimJobAsync(Guid jobId, string workerId, CancellationToken cancellationToken);
    Task<Result> HeartbeatAsync(Guid jobId, string workerId, Guid executionId, CancellationToken cancellationToken);
}
