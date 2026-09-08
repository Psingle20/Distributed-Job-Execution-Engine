using Microsoft.EntityFrameworkCore;
using WorkerService.Application.Abstractions;
using WorkerService.Domain.Workers;

namespace WorkerService.Infrastructure.Persistence.Repositories;

internal sealed class WorkerExecutionRepository(WorkerDbContext dbContext) : IWorkerExecutionRepository
{
    public Task<WorkerExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.WorkerExecutions.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public Task<WorkerExecution?> GetByJobIdAndAttemptAsync(Guid jobId, int attemptNumber, CancellationToken cancellationToken)
    {
        return dbContext.WorkerExecutions
            .FirstOrDefaultAsync(e => e.JobId == jobId && e.AttemptNumber == attemptNumber, cancellationToken);
    }

    public async Task AddAsync(WorkerExecution execution, CancellationToken cancellationToken)
    {
        await dbContext.WorkerExecutions.AddAsync(execution, cancellationToken);
    }

    public Task<int> GetActiveCountAsync(CancellationToken cancellationToken)
    {
        return dbContext.WorkerExecutions
            .CountAsync(e => e.Status == ExecutionStatus.Running, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkerExecution>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await dbContext.WorkerExecutions
            .Where(e => e.Status == ExecutionStatus.Running)
            .ToListAsync(cancellationToken);
    }
}
