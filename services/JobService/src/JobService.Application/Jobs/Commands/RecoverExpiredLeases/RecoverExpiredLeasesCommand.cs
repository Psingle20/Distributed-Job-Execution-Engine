using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Commands.RecoverExpiredLeases;

public sealed record RecoverExpiredLeasesCommand : ICommand;
