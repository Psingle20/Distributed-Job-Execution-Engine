using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Dtos;

public sealed class RetryJobDto : IDto
{
    public Guid JobId { get; set; }
}
