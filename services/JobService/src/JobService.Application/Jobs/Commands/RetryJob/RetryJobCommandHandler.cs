using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobEngine.Contracts.IntegrationEvents;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.RetryJob;

internal sealed class RetryJobCommandHandler(
    IJobRepository jobRepository,
    IOutboxEventPublisher outbox,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RetryJobCommand>
{
    public async Task<Result> Handle(RetryJobCommand command, CancellationToken cancellationToken)
    {
        Job? job = await jobRepository.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound(command.JobId));
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        Result result = job.RetryFromFailed(now);
        if (result.IsFailure)
        {
            return result;
        }

        outbox.Enqueue("jobs.retry-scheduled", new JobRetryScheduledIntegrationEvent(
            job.Id, job.Type, job.PayloadJson, job.AttemptCount, now));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
