namespace JobEngine.Contracts.IntegrationEvents;

public sealed record JobCancelledIntegrationEvent(
    Guid JobId,
    DateTimeOffset CancelledAt);
