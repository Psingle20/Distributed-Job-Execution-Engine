using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Queries;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Dtos;

public sealed class SearchJobsDto : IDto<IReadOnlyList<JobResponse>>
{
    public JobState? State { get; set; }
    public string? Type { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
