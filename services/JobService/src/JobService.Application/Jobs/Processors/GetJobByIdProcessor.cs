using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Dtos;
using JobService.Application.Jobs.Queries;
using JobService.Application.Jobs.Queries.GetJobById;

namespace JobService.Application.Jobs.Processors;

internal sealed class GetJobByIdProcessor(IQueryDispatcher queryDispatcher)
    : IProcessor<GetJobByIdDto, JobResponse>
{
    public Task<Result<JobResponse>> Process(GetJobByIdDto dto, CancellationToken cancellationToken)
    {
        var query = new GetJobByIdQuery(dto.JobId);
        return queryDispatcher.Dispatch(query, cancellationToken);
    }
}
