using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobEngine.Contracts.IntegrationEvents;
using WorkerService.Application.Abstractions;
using WorkerService.Domain.Workers;

namespace WorkerService.Application.Jobs.Commands.HandleJobRetryScheduled;

internal sealed class HandleJobRetryScheduledCommandHandler(
    IProcessedMessageRepository processedMessages,
    IJobServiceClient jobServiceClient,
    IJobHandlerRegistry handlerRegistry,
    IWorkerExecutionRepository executionRepository,
    IOutboxEventPublisher outbox,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<HandleJobRetryScheduledCommand>
{
    private static readonly string WorkerId = $"worker-{Environment.MachineName}-{Environment.ProcessId}";

    public async Task<Result> Handle(HandleJobRetryScheduledCommand command, CancellationToken cancellationToken)
    {
        if (await processedMessages.ExistsAsync(command.MessageId, cancellationToken))
        {
            return Result.Success();
        }

        IJobHandler? handler = handlerRegistry.GetHandler(command.JobType);
        if (handler is null)
        {
            return Result.Success();
        }

        Result claimResult = await jobServiceClient.ClaimJobAsync(command.JobId, WorkerId, cancellationToken);
        if (claimResult.IsFailure)
        {
            return Result.Success();
        }

        var execution = WorkerExecution.Create(
            command.JobId, command.JobType, WorkerId, command.AttemptNumber, dateTimeProvider.UtcNow);
        await executionRepository.AddAsync(execution, cancellationToken);

        var context = new JobExecutionContext(
            command.JobId, command.JobType, command.Payload, command.AttemptNumber, WorkerId);

        try
        {
            string? resultPayload = await handler.ExecuteAsync(context, cancellationToken);
            execution.MarkCompleted(resultPayload, dateTimeProvider.UtcNow);

            outbox.Enqueue("job-executions.completed", new JobExecutionCompletedIntegrationEvent(
                command.JobId, WorkerId, command.AttemptNumber, resultPayload, dateTimeProvider.UtcNow));
        }
        catch (Exception ex)
        {
            execution.MarkFailed(ex.Message, dateTimeProvider.UtcNow);

            outbox.Enqueue("job-executions.failed", new JobExecutionFailedIntegrationEvent(
                command.JobId, WorkerId, command.AttemptNumber, ex.Message, true, dateTimeProvider.UtcNow));
        }

        await processedMessages.AddAsync(command.MessageId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
