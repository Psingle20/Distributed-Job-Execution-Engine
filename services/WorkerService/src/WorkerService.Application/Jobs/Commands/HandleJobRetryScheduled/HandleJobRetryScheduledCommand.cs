using CleanArchitecture.BuildingBlocks.Messaging;

namespace WorkerService.Application.Jobs.Commands.HandleJobRetryScheduled;

public sealed record HandleJobRetryScheduledCommand(
    string MessageId,
    Guid JobId,
    string JobType,
    string Payload,
    int AttemptNumber,
    DateTimeOffset RetryAt) : ICommand;
