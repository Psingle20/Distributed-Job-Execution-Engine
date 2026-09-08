using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Queries.SearchJobs;

internal sealed class SearchJobsQueryHandler(IJobRepository jobRepository)
    : IQueryHandler<SearchJobsQuery, IReadOnlyList<JobResponse>>
{
    public async Task<Result<IReadOnlyList<JobResponse>>> Handle(SearchJobsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<Job> jobs = await jobRepository.SearchAsync(
            query.State, query.Type, query.Page, query.PageSize, cancellationToken);

        IReadOnlyList<JobResponse> response = jobs.Select(MapToResponse).ToList();
        return Result.Success(response);
    }

    private static JobResponse MapToResponse(Job job) =>
        new(
            job.Id,
            job.Type,
            job.PayloadJson,
            job.State,
            job.AttemptCount,
            job.MaxAttempts,
            job.WorkerId,
            job.LeaseUntil,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt,
            job.FailedAt,
            job.LastError);
}
