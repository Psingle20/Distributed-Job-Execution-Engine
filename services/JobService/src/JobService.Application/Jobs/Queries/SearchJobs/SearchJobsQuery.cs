using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Queries.SearchJobs;

public sealed record SearchJobsQuery(
    JobState? State,
    string? Type,
    int Page,
    int PageSize) : IQuery<IReadOnlyList<JobResponse>>;
