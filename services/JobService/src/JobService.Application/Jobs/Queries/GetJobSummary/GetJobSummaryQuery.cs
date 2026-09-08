using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Queries.GetJobSummary;

public sealed record GetJobSummaryQuery : IQuery<Dictionary<JobState, int>>;
