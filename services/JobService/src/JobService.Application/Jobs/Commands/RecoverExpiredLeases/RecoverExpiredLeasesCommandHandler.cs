using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobEngine.Contracts.IntegrationEvents;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.RecoverExpiredLeases;

internal sealed class RecoverExpiredLeasesCommandHandler(
    IJobRepository jobRepository,
    IOutboxEventPublisher outbox,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RecoverExpiredLeasesCommand>
{
    public async Task<Result> Handle(RecoverExpiredLeasesCommand command, CancellationToken cancellationToken)
    {
        DateTimeOffset now = dateTimeProvider.UtcNow;
        IReadOnlyList<Job> expiredJobs = await jobRepository.GetExpiredLeasesAsync(now, cancellationToken);

        foreach (Job job in expiredJobs)
        {
            TimeSpan delay = JobRetryPolicy.CalculateDelay(job.AttemptCount);
            DateTimeOffset retryAt = now + delay;
            job.MarkLeaseExpired(now, retryAt);

            if (job.State == JobState.Retrying)
            {
                outbox.Enqueue("jobs.retry-scheduled", new JobRetryScheduledIntegrationEvent(
                    job.Id, job.Type, job.PayloadJson, job.AttemptCount, retryAt));
            }
        }

        if (expiredJobs.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
