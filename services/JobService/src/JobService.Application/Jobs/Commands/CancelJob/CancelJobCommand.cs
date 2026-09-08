using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Commands.CancelJob;

public sealed record CancelJobCommand(Guid JobId) : ICommand;
