using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Queries.GetJobById;

internal sealed class GetJobByIdQueryHandler(IJobRepository jobRepository)
    : IQueryHandler<GetJobByIdQuery, JobResponse>
{
    public async Task<Result<JobResponse>> Handle(GetJobByIdQuery query, CancellationToken cancellationToken)
    {
        Job? job = await jobRepository.GetByIdAsync(query.JobId, cancellationToken);
        if (job is null)
        {
            return Result.Failure<JobResponse>(JobErrors.NotFound(query.JobId));
        }

        return Result.Success(MapToResponse(job));
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
