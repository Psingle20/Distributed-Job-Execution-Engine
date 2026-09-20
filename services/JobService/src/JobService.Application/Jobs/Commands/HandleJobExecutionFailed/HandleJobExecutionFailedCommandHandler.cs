using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Abstractions;
using JobService.Application.Observability;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.HandleJobExecutionFailed;

internal sealed class HandleJobExecutionFailedCommandHandler(
    IJobRepository jobRepository,
    IProcessedMessageRepository processedMessages,
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

        Result result = job.Fail(command.WorkerId, command.ExecutionId, command.FailedAt, command.ErrorMessage);
        if (result.IsFailure)
        {
            return result;
        }

        await processedMessages.AddAsync(command.MessageId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (job.State == JobState.Failed)
        {
            JobServiceDiagnostics.JobsFailed.Add(1);
        }

        return Result.Success();
    }
}
