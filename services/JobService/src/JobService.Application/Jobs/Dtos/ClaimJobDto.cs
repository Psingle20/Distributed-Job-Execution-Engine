using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Dtos;

public sealed class ClaimJobDto : IDto
{
    public Guid JobId { get; set; }
    public string WorkerId { get; set; } = string.Empty;
}
