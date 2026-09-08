namespace JobEngine.Contracts.IntegrationEvents;

public sealed record JobCompletedIntegrationEvent(
    Guid JobId,
    string JobType,
    int TotalAttempts,
    string? ResultPayload,
    DateTimeOffset CompletedAt);
