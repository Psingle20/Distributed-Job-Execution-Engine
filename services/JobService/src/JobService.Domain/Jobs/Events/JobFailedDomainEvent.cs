using CleanArchitecture.BuildingBlocks;

namespace JobService.Domain.Jobs.Events;

public sealed record JobFailedDomainEvent(Guid JobId, string? LastError, DateTimeOffset FailedAt) : IDomainEvent;
