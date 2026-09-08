using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobEngine.Contracts.IntegrationEvents;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.HandleJobExecutionFailed;

internal sealed class HandleJobExecutionFailedCommandHandler(
    IJobRepository jobRepository,
    IProcessedMessageRepository processedMessages,
    IOutboxEventPublisher outbox,
    IUnitOfWork unitOfWork) : ICommandHandler<HandleJobExecutionFailedCommand>
{
    public async Task<Result> Handle(HandleJobExecutionFailedCommand command, CancellationToken cancellationToken)
    {
        if (await processedMessages.ExistsAsync(command.MessageId, cancellationToken))
        {
            return Result.Success();
        }

        Job? job = await jobRepository.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound(command.JobId));
        }

        Result result = job.Fail(command.WorkerId, command.FailedAt, command.ErrorMessage);
        if (result.IsFailure)
        {
            return result;
        }

        if (job.State == JobState.Retrying && job.NextRunAt is not null)
        {
            outbox.Enqueue("jobs.retry-scheduled", new JobRetryScheduledIntegrationEvent(
                job.Id, job.Type, job.PayloadJson, job.AttemptCount, job.NextRunAt.Value));
        }

        await processedMessages.AddAsync(command.MessageId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
