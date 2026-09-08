using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Dtos;

public sealed class GetJobSummaryDto : IDto<Dictionary<JobState, int>>;
