using JobService.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobService.Infrastructure.Persistence.Configurations;

internal sealed class JobExecutionConfiguration : IEntityTypeConfiguration<JobExecution>
{
    public void Configure(EntityTypeBuilder<JobExecution> builder)
    {
        builder.ToTable("job_executions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.JobId).IsRequired();
        builder.Property(e => e.WorkerId).HasMaxLength(128).IsRequired();
        builder.Property(e => e.AttemptNumber).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).HasConversion<string>().IsRequired();
        builder.Property(e => e.StartedAt).IsRequired();
        builder.Property(e => e.CompletedAt);
        builder.Property(e => e.Error).HasColumnType("text");

        builder.HasIndex(e => e.JobId).HasDatabaseName("IX_job_executions_job_id");
        builder.HasIndex(e => new { e.JobId, e.AttemptNumber })
            .IsUnique()
            .HasDatabaseName("UX_job_executions_job_id_attempt_number");
    }
}
