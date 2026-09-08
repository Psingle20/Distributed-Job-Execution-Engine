using JobService.Application.Abstractions;
using JobService.Domain.Jobs;
using Microsoft.EntityFrameworkCore;

namespace JobService.Infrastructure.Persistence.Repositories;

internal sealed class JobRepository(JobDbContext dbContext) : IJobRepository
{
    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Jobs.FindAsync([id], cancellationToken);
    }

    public async Task<Job?> GetByIdWithExecutionsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Jobs
            .Include(j => j.Executions)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<Job?> GetByIdWithTransitionsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Jobs
            .Include(j => j.StateTransitions)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task AddAsync(Job job, CancellationToken cancellationToken)
    {
        await dbContext.Jobs.AddAsync(job, cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> GetExpiredLeasesAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await dbContext.Jobs
            .Where(j => j.State == JobState.Running && j.LeaseUntil != null && j.LeaseUntil < now)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> SearchAsync(
        JobState? state,
        string? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Job> query = dbContext.Jobs.AsQueryable();

        if (state.HasValue)
        {
            query = query.Where(j => j.State == state.Value);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(j => j.Type == type);
        }

        return await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<JobState, int>> GetSummaryCountsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Jobs
            .GroupBy(j => j.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.State, x => x.Count, cancellationToken);
    }
}
