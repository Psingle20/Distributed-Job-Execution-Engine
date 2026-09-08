using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Commands.HandleJobExecutionCompleted;

public sealed record HandleJobExecutionCompletedCommand(
    string MessageId,
    Guid JobId,
    string WorkerId,
    int AttemptNumber,
    string? ResultPayload,
    DateTimeOffset CompletedAt) : ICommand;
