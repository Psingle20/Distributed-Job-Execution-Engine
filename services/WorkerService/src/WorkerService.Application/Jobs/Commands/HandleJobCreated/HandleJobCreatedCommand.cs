using CleanArchitecture.BuildingBlocks.Messaging;

namespace WorkerService.Application.Jobs.Commands.HandleJobCreated;

public sealed record HandleJobCreatedCommand(
    string MessageId,
    Guid JobId,
    string JobType,
    string Payload,
    int MaxAttempts,
    DateTimeOffset CreatedAt) : ICommand;
