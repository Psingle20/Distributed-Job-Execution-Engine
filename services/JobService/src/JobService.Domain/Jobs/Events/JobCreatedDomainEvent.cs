using CleanArchitecture.BuildingBlocks;

namespace JobService.Domain.Jobs.Events;

public sealed record JobCreatedDomainEvent(Guid JobId, string JobType, DateTimeOffset CreatedAt) : IDomainEvent;
