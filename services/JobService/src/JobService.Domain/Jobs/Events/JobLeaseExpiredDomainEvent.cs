using CleanArchitecture.BuildingBlocks;

namespace JobService.Domain.Jobs.Events;

public sealed record JobLeaseExpiredDomainEvent(Guid JobId, string? PreviousWorkerId, DateTimeOffset ExpiredAt) : IDomainEvent;
