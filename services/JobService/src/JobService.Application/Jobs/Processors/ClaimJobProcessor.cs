using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Commands.ClaimJob;
using JobService.Application.Jobs.Dtos;

namespace JobService.Application.Jobs.Processors;

internal sealed class ClaimJobProcessor(ICommandDispatcher commandDispatcher)
    : IProcessor<ClaimJobDto, ClaimJobResult>
{
    public Task<Result<ClaimJobResult>> Process(ClaimJobDto dto, CancellationToken cancellationToken)
    {
        var command = new ClaimJobCommand(dto.JobId, dto.WorkerId);
        return commandDispatcher.Dispatch<ClaimJobResult>(command, cancellationToken);
    }
}
