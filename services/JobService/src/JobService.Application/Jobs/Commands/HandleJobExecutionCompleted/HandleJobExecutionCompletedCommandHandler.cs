using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Abstractions;
using JobService.Application.Observability;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.HandleJobExecutionCompleted;

internal sealed class HandleJobExecutionCompletedCommandHandler(
    IJobRepository jobRepository,
    IProcessedMessageRepository processedMessages,
    IUnitOfWork unitOfWork) : ICommandHandler<HandleJobExecutionCompletedCommand>
{
    public async Task<Result> Handle(HandleJobExecutionCompletedCommand command, CancellationToken cancellationToken)
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

        Result result = job.Complete(command.WorkerId, command.ExecutionId, command.CompletedAt, command.ResultPayload);
        if (result.IsFailure)
        {
            return result;
        }

        await processedMessages.AddAsync(command.MessageId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        JobServiceDiagnostics.JobsCompleted.Add(1);
        return Result.Success();
    }
}
