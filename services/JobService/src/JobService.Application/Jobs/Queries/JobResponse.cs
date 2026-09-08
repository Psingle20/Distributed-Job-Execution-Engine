using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Queries;

public sealed record JobResponse(
    Guid Id,
    string Type,
    string PayloadJson,
    JobState State,
    int AttemptCount,
    int MaxAttempts,
    string? WorkerId,
    DateTimeOffset? LeaseUntil,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? FailedAt,
    string? LastError);
