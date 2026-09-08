namespace JobEngine.Contracts.IntegrationEvents;

public sealed record JobRetryScheduledIntegrationEvent(
    Guid JobId,
    string JobType,
    string Payload,
    int AttemptNumber,
    DateTimeOffset RetryAt);
