using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Commands.CreateJob;

public sealed record CreateJobCommand(
    string Type,
    string PayloadJson,
    int MaxAttempts) : ICommand<Guid>;
