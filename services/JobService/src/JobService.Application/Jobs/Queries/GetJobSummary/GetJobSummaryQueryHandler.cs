using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Queries.GetJobSummary;

internal sealed class GetJobSummaryQueryHandler(IJobRepository jobRepository)
    : IQueryHandler<GetJobSummaryQuery, Dictionary<JobState, int>>
{
    public async Task<Result<Dictionary<JobState, int>>> Handle(
        GetJobSummaryQuery query,
        CancellationToken cancellationToken)
    {
        Dictionary<JobState, int> counts = await jobRepository.GetSummaryCountsAsync(cancellationToken);
        return Result.Success(counts);
    }
}
