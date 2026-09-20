using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Commands.HeartbeatJob;

public sealed record HeartbeatJobCommand(Guid JobId, string WorkerId, Guid ExecutionId) : ICommand;
