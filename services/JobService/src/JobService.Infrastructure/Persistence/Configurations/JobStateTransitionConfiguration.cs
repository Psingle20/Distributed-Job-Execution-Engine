using JobService.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobService.Infrastructure.Persistence.Configurations;

internal sealed class JobStateTransitionConfiguration : IEntityTypeConfiguration<JobStateTransition>
{
    public void Configure(EntityTypeBuilder<JobStateTransition> builder)
    {
        builder.ToTable("job_state_transitions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.JobId).IsRequired();
        builder.Property(t => t.FromState).HasMaxLength(32).HasConversion<string>();
        builder.Property(t => t.ToState).HasMaxLength(32).HasConversion<string>().IsRequired();
        builder.Property(t => t.Reason).HasMaxLength(256);
        builder.Property(t => t.WorkerId).HasMaxLength(128);
        builder.Property(t => t.OccurredAt).IsRequired();

        builder.HasIndex(t => new { t.JobId, t.OccurredAt })
            .HasDatabaseName("IX_job_state_transitions_job_id_occurred_at");
    }
}
