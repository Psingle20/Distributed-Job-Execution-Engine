using JobService.Application.Abstractions;

namespace JobService.Infrastructure.Persistence;

internal sealed class UnitOfWork(JobDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
