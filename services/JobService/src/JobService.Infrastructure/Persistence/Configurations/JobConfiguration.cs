using JobService.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobService.Infrastructure.Persistence.Configurations;

internal sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();

        builder.Property(j => j.Type).HasMaxLength(128).IsRequired();
        builder.Property(j => j.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(j => j.State).HasMaxLength(32).HasConversion<string>().IsRequired();
        builder.Property(j => j.AttemptCount).IsRequired();
        builder.Property(j => j.MaxAttempts).IsRequired();
        builder.Property(j => j.ExecutionId);
        builder.Property(j => j.WorkerId).HasMaxLength(128);
        builder.Property(j => j.LeaseUntil);
        builder.Property(j => j.LastHeartbeatAt);
        builder.Property(j => j.NextRunAt);
        builder.Property(j => j.CreatedAt).IsRequired();
        builder.Property(j => j.StartedAt);
        builder.Property(j => j.CompletedAt);
        builder.Property(j => j.FailedAt);
        builder.Property(j => j.LastError).HasColumnType("text");

        builder.Property(j => j.Version)
            .IsRowVersion();

        builder.HasIndex(j => new { j.State, j.NextRunAt }).HasDatabaseName("IX_jobs_state_next_run_at");
        builder.HasIndex(j => j.LeaseUntil).HasDatabaseName("IX_jobs_lease_until");
        builder.HasIndex(j => j.WorkerId).HasDatabaseName("IX_jobs_worker_id");
        builder.HasIndex(j => new { j.Type, j.CreatedAt }).HasDatabaseName("IX_jobs_type_created_at");

        builder.HasMany(j => j.Executions)
            .WithOne()
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(j => j.StateTransitions)
            .WithOne()
            .HasForeignKey(t => t.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(j => j.DomainEvents);

        builder.Navigation(j => j.Executions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(j => j.StateTransitions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
