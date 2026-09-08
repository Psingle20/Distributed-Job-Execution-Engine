using Dapr.EntityFrameworkCore.Outbox;
using JobService.Domain.Jobs;
using Microsoft.EntityFrameworkCore;

namespace JobService.Infrastructure.Persistence;

public sealed class JobDbContext(DbContextOptions<JobDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();

    public DbSet<JobStateTransition> JobStateTransitions => Set<JobStateTransition>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobDbContext).Assembly);
        modelBuilder.AddDaprOutbox();
    }
}
