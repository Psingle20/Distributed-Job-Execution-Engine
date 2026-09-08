using CleanArchitecture.BuildingBlocks;

namespace JobService.Domain.Jobs.Events;

public sealed record JobRetryScheduledDomainEvent(
    Guid JobId,
    int AttemptNumber,
    DateTimeOffset RetryAt) : IDomainEvent;
