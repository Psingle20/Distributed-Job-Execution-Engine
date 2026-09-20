namespace JobService.Application.Jobs.Commands.ClaimJob;

public sealed record ClaimJobResult(Guid ExecutionId, int AttemptNumber, DateTimeOffset LeaseExpiresAt);
