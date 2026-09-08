namespace WorkerService.Infrastructure.Persistence;

public sealed class ProcessedMessage
{
    public string MessageId { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}
