using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Queries;

namespace JobService.Application.Jobs.Dtos;

public sealed class GetJobByIdDto : IDto<JobResponse>
{
    public Guid JobId { get; set; }
}
