using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Commands.HandleJobExecutionCompleted;

public sealed record HandleJobExecutionCompletedCommand(
    string MessageId,
    Guid JobId,
    string WorkerId,
    Guid ExecutionId,
    int AttemptNumber,
    string? ResultPayload,
    DateTimeOffset CompletedAt) : ICommand;
