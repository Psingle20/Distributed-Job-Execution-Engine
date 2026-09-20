using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Commands.ClaimJob;

namespace JobService.Application.Jobs.Dtos;

public sealed class ClaimJobDto : IDto<ClaimJobResult>
{
    public Guid JobId { get; set; }
    public string WorkerId { get; set; } = string.Empty;
}
