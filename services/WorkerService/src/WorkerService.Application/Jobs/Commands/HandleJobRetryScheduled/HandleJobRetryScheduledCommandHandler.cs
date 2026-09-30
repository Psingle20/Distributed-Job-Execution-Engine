using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobEngine.Contracts.IntegrationEvents;
using WorkerService.Application.Abstractions;
using WorkerService.Application.Observability;
using WorkerService.Domain.Workers;

namespace WorkerService.Application.Jobs.Commands.HandleJobRetryScheduled;

internal sealed class HandleJobRetryScheduledCommandHandler(
    IProcessedMessageRepository processedMessages,
    IJobServiceClient jobServiceClient,
    IJobHandlerRegistry handlerRegistry,
    IWorkerExecutionRepository executionRepository,
    IOutboxEventPublisher outbox,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IChaosHook chaosHook) : ICommandHandler<HandleJobRetryScheduledCommand>
{
    private static readonly string WorkerId = $"worker-{Environment.MachineName}-{Environment.ProcessId}";

    public async Task<Result> Handle(HandleJobRetryScheduledCommand command, CancellationToken cancellationToken)
    {
        if (await processedMessages.ExistsAsync(command.MessageId, cancellationToken))
        {
            WorkerServiceDiagnostics.DuplicatesSkipped.Add(1);
            return Result.Success();
        }

        IJobHandler? handler = handlerRegistry.GetHandler(command.JobType);
        if (handler is null)
        {
            Result<ClaimResult> unknownClaim = await jobServiceClient.ClaimJobAsync(command.JobId, WorkerId, cancellationToken);
            if (unknownClaim.IsSuccess)
            {
                outbox.Enqueue("job-executions.failed", new JobExecutionFailedIntegrationEvent(
                    command.JobId, WorkerId, unknownClaim.Value.ExecutionId, unknownClaim.Value.AttemptNumber,
                    $"No handler registered for job type '{command.JobType}'", false, dateTimeProvider.UtcNow));

                await processedMessages.AddAsync(command.MessageId, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }

        Result<ClaimResult> claimResult = await jobServiceClient.ClaimJobAsync(command.JobId, WorkerId, cancellationToken);
        if (claimResult.IsFailure)
        {
            WorkerServiceDiagnostics.ClaimConflicts.Add(1);
            return Result.Success();
        }

        ClaimResult claim = claimResult.Value;
        WorkerServiceDiagnostics.JobsClaimed.Add(1);

        var execution = WorkerExecution.Create(
            command.JobId, command.JobType, WorkerId, claim.AttemptNumber, claim.ExecutionId, dateTimeProvider.UtcNow);
        await executionRepository.AddAsync(execution, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            chaosHook.Check("crash-after-claim");
        }
        catch (Exception)
        {
            execution.MarkFailed("SIMULATED CRASH after claim", dateTimeProvider.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var context = new JobExecutionContext(
            command.JobId, command.JobType, command.Payload, claim.AttemptNumber, WorkerId, claim.ExecutionId);

        try
        {
            chaosHook.CheckOnce("fail-next-job");

            string? resultPayload = await handler.ExecuteAsync(context, cancellationToken);
            execution.MarkCompleted(resultPayload, dateTimeProvider.UtcNow);

            outbox.Enqueue("job-executions.completed", new JobExecutionCompletedIntegrationEvent(
                command.JobId, WorkerId, claim.ExecutionId, claim.AttemptNumber, resultPayload, dateTimeProvider.UtcNow));

            WorkerServiceDiagnostics.JobsExecuted.Add(1);
        }
        catch (Exception ex)
        {
            execution.MarkFailed(ex.Message, dateTimeProvider.UtcNow);

            outbox.Enqueue("job-executions.failed", new JobExecutionFailedIntegrationEvent(
                command.JobId, WorkerId, claim.ExecutionId, claim.AttemptNumber, ex.Message, true, dateTimeProvider.UtcNow));

            WorkerServiceDiagnostics.JobsFailed.Add(1);
        }

        chaosHook.Check("crash-after-effect");

        await processedMessages.AddAsync(command.MessageId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
