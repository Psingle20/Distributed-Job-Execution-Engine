using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Dtos;
using JobService.Application.Jobs.Queries;
using JobService.Application.Jobs.Queries.SearchJobs;

namespace JobService.Application.Jobs.Processors;

internal sealed class SearchJobsProcessor(IQueryDispatcher queryDispatcher)
    : IProcessor<SearchJobsDto, IReadOnlyList<JobResponse>>
{
    public Task<Result<IReadOnlyList<JobResponse>>> Process(SearchJobsDto dto, CancellationToken cancellationToken)
    {
        var query = new SearchJobsQuery(dto.State, dto.Type, dto.Page, dto.PageSize);
        return queryDispatcher.Dispatch(query, cancellationToken);
    }
}
