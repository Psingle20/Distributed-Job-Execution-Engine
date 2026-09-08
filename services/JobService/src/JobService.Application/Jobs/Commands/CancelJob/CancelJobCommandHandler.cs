using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobEngine.Contracts.IntegrationEvents;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.CancelJob;

internal sealed class CancelJobCommandHandler(
    IJobRepository jobRepository,
    IOutboxEventPublisher outbox,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CancelJobCommand>
{
    public async Task<Result> Handle(CancelJobCommand command, CancellationToken cancellationToken)
    {
        Job? job = await jobRepository.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound(command.JobId));
        }

        Result result = job.Cancel(dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        outbox.Enqueue("jobs.cancelled", new JobCancelledIntegrationEvent(job.Id, dateTimeProvider.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
