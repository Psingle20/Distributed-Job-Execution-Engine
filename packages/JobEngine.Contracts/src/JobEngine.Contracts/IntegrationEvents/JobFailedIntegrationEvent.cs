namespace JobEngine.Contracts.IntegrationEvents;

public sealed record JobFailedIntegrationEvent(
    Guid JobId,
    string JobType,
    int TotalAttempts,
    string LastErrorMessage,
    DateTimeOffset FailedAt);
