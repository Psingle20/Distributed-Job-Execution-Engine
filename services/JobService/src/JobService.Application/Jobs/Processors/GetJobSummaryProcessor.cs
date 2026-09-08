using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Dtos;
using JobService.Application.Jobs.Queries.GetJobSummary;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Processors;

internal sealed class GetJobSummaryProcessor(IQueryDispatcher queryDispatcher)
    : IProcessor<GetJobSummaryDto, Dictionary<JobState, int>>
{
    public Task<Result<Dictionary<JobState, int>>> Process(GetJobSummaryDto dto, CancellationToken cancellationToken)
    {
        var query = new GetJobSummaryQuery();
        return queryDispatcher.Dispatch(query, cancellationToken);
    }
}
