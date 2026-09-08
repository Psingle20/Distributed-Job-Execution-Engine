using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Dtos;

public sealed class CancelJobDto : IDto
{
    public Guid JobId { get; set; }
}
