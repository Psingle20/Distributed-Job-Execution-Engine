using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Commands.HandleJobExecutionFailed;

public sealed record HandleJobExecutionFailedCommand(
    string MessageId,
    Guid JobId,
    string WorkerId,
    Guid ExecutionId,
    int AttemptNumber,
    string ErrorMessage,
    bool IsRetryable,
    DateTimeOffset FailedAt) : ICommand;
