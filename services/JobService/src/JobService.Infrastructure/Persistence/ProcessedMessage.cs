namespace JobService.Infrastructure.Persistence;

public sealed class ProcessedMessage
{
    public string MessageId { get; init; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; init; }
}
