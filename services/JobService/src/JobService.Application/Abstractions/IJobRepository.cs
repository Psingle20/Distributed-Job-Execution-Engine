using JobService.Domain.Jobs;

namespace JobService.Application.Abstractions;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Job?> GetByIdWithExecutionsAsync(Guid id, CancellationToken cancellationToken);

    Task<Job?> GetByIdWithTransitionsAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Job job, CancellationToken cancellationToken);

    Task<IReadOnlyList<Job>> GetExpiredLeasesAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<Job>> SearchAsync(
        JobState? state,
        string? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Dictionary<JobState, int>> GetSummaryCountsAsync(CancellationToken cancellationToken);
}
