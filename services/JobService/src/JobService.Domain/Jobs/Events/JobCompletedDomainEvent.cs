using CleanArchitecture.BuildingBlocks;

namespace JobService.Domain.Jobs.Events;

public sealed record JobCompletedDomainEvent(Guid JobId, string WorkerId, DateTimeOffset CompletedAt) : IDomainEvent;
