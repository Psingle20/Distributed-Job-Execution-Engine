using Microsoft.EntityFrameworkCore;
using WorkerService.Application.Abstractions;

namespace WorkerService.Infrastructure.Persistence.Repositories;

internal sealed class ProcessedMessageRepository(WorkerDbContext dbContext) : IProcessedMessageRepository
{
    public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken)
    {
        return dbContext.ProcessedMessages.AnyAsync(p => p.MessageId == messageId, cancellationToken);
    }

    public async Task AddAsync(string messageId, CancellationToken cancellationToken)
    {
        await dbContext.ProcessedMessages.AddAsync(
            new ProcessedMessage { MessageId = messageId, ProcessedAt = DateTimeOffset.UtcNow },
            cancellationToken);
    }
}
