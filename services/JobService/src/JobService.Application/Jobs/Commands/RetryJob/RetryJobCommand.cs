using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Commands.RetryJob;

public sealed record RetryJobCommand(Guid JobId) : ICommand;
