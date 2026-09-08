namespace JobEngine.Contracts.IntegrationEvents;

public sealed record JobExecutionFailedIntegrationEvent(
    Guid JobId,
    string WorkerId,
    int AttemptNumber,
    string ErrorMessage,
    bool IsRetryable,
    DateTimeOffset FailedAt);
