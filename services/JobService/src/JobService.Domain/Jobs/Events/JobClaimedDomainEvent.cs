using CleanArchitecture.BuildingBlocks;

namespace JobService.Domain.Jobs.Events;

public sealed record JobClaimedDomainEvent(Guid JobId, string WorkerId, int AttemptNumber) : IDomainEvent;
