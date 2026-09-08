using WorkerService.Application.Abstractions;

namespace WorkerService.Infrastructure.Persistence;

internal sealed class UnitOfWork(WorkerDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
