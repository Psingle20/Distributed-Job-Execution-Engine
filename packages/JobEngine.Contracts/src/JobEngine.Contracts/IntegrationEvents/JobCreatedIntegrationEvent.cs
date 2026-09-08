namespace JobEngine.Contracts.IntegrationEvents;

public sealed record JobCreatedIntegrationEvent(
    Guid JobId,
    string JobType,
    string Payload,
    int MaxAttempts,
    DateTimeOffset CreatedAt);
