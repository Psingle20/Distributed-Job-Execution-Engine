using CleanArchitecture.BuildingBlocks;

namespace JobService.Domain.Jobs.Events;

public sealed record JobCancelledDomainEvent(Guid JobId, DateTimeOffset CancelledAt) : IDomainEvent;
