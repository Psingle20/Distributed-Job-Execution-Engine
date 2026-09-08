using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Commands.CancelJob;
using JobService.Application.Jobs.Dtos;

namespace JobService.Application.Jobs.Processors;

internal sealed class CancelJobProcessor(ICommandDispatcher commandDispatcher)
    : IProcessor<CancelJobDto>
{
    public Task<Result> Process(CancelJobDto dto, CancellationToken cancellationToken)
    {
        var command = new CancelJobCommand(dto.JobId);
        return commandDispatcher.Dispatch(command, cancellationToken);
    }
}
