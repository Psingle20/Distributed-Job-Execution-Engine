namespace JobEngine.Contracts.IntegrationEvents;

public sealed record JobExecutionCompletedIntegrationEvent(
    Guid JobId,
    string WorkerId,
    int AttemptNumber,
    string? ResultPayload,
    DateTimeOffset CompletedAt);
