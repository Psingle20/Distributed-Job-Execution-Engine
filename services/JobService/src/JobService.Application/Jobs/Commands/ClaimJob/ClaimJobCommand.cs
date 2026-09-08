using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Commands.ClaimJob;

public sealed record ClaimJobCommand(Guid JobId, string WorkerId) : ICommand;
