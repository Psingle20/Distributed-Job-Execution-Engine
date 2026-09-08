using Dapr.EntityFrameworkCore.Outbox;
using Microsoft.EntityFrameworkCore;
using WorkerService.Domain.Workers;

namespace WorkerService.Infrastructure.Persistence;

public sealed class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options)
{
    public DbSet<WorkerExecution> WorkerExecutions => Set<WorkerExecution>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkerDbContext).Assembly);
        modelBuilder.AddDaprOutbox();
    }
}
