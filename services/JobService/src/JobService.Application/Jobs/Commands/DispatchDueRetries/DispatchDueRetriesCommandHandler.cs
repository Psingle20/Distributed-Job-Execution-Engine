using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobEngine.Contracts.IntegrationEvents;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.DispatchDueRetries;

internal sealed class DispatchDueRetriesCommandHandler(
    IJobRepository jobRepository,
    IOutboxEventPublisher outbox,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DispatchDueRetriesCommand>
{
    public async Task<Result> Handle(DispatchDueRetriesCommand command, CancellationToken cancellationToken)
    {
        DateTimeOffset now = dateTimeProvider.UtcNow;
        IReadOnlyList<Job> dueJobs = await jobRepository.GetDueRetryJobsAsync(now, cancellationToken);

        foreach (Job job in dueJobs)
        {
            outbox.Enqueue("jobs.retry-scheduled", new JobRetryScheduledIntegrationEvent(
                job.Id, job.Type, job.PayloadJson, job.AttemptCount, now));
        }

        if (dueJobs.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
