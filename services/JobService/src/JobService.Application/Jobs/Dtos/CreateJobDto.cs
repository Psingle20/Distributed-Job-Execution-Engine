using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Dtos;

public sealed class CreateJobDto : IDto<Guid>
{
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public int MaxAttempts { get; set; }
}
