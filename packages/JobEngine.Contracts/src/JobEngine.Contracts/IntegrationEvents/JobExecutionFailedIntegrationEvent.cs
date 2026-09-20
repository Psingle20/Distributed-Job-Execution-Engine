namespace JobEngine.Contracts.IntegrationEvents;

public sealed record JobExecutionFailedIntegrationEvent(
    Guid JobId,
    string WorkerId,
    Guid ExecutionId,
    int AttemptNumber,
    string ErrorMessage,
    bool IsRetryable,
    DateTimeOffset FailedAt);
