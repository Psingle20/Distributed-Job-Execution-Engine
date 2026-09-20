using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Commands.HeartbeatJob;
using JobService.Application.Jobs.Dtos;

namespace JobService.Application.Jobs.Processors;

internal sealed class HeartbeatJobProcessor(ICommandDispatcher commandDispatcher)
    : IProcessor<HeartbeatJobDto>
{
    public Task<Result> Process(HeartbeatJobDto dto, CancellationToken cancellationToken)
    {
        var command = new HeartbeatJobCommand(dto.JobId, dto.WorkerId, dto.ExecutionId);
        return commandDispatcher.Dispatch(command, cancellationToken);
    }
}
