namespace WorkerService.Application.Abstractions;

public interface IProcessedMessageRepository
{
    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken);
    Task AddAsync(string messageId, CancellationToken cancellationToken);
}
