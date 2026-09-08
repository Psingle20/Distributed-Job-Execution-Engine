using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Commands.RetryJob;
using JobService.Application.Jobs.Dtos;

namespace JobService.Application.Jobs.Processors;

internal sealed class RetryJobProcessor(ICommandDispatcher commandDispatcher)
    : IProcessor<RetryJobDto>
{
    public Task<Result> Process(RetryJobDto dto, CancellationToken cancellationToken)
    {
        var command = new RetryJobCommand(dto.JobId);
        return commandDispatcher.Dispatch(command, cancellationToken);
    }
}
