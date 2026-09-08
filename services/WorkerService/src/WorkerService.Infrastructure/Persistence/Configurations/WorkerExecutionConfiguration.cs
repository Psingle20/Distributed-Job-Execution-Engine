using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkerService.Domain.Workers;

namespace WorkerService.Infrastructure.Persistence.Configurations;

internal sealed class WorkerExecutionConfiguration : IEntityTypeConfiguration<WorkerExecution>
{
    public void Configure(EntityTypeBuilder<WorkerExecution> builder)
    {
        builder.ToTable("worker_executions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.JobType).HasMaxLength(128);
        builder.Property(e => e.WorkerId).HasMaxLength(256);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ResultPayload).HasColumnType("jsonb");

        builder.HasIndex(e => new { e.JobId, e.AttemptNumber }).IsUnique();
        builder.HasIndex(e => e.Status);

        builder.Ignore(e => e.DomainEvents);
    }
}
