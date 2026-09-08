using CleanArchitecture.BuildingBlocks;

namespace WorkerService.Application.Abstractions;

public interface IJobServiceClient
{
    Task<Result> ClaimJobAsync(Guid jobId, string workerId, CancellationToken cancellationToken);
    Task<Result> HeartbeatAsync(Guid jobId, string workerId, CancellationToken cancellationToken);
}
