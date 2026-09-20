namespace JobEngine.Contracts.IntegrationEvents;

public sealed record JobExecutionCompletedIntegrationEvent(
    Guid JobId,
    string WorkerId,
    Guid ExecutionId,
    int AttemptNumber,
    string? ResultPayload,
    DateTimeOffset CompletedAt);
