using CleanArchitecture.BuildingBlocks.Messaging;

namespace WorkerService.Application.Jobs.Commands.HandleJobCancelled;

public sealed record HandleJobCancelledCommand(
    string MessageId,
    Guid JobId,
    DateTimeOffset CancelledAt) : ICommand;
