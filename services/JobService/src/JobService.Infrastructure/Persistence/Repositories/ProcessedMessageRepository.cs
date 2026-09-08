using JobService.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace JobService.Infrastructure.Persistence.Repositories;

internal sealed class ProcessedMessageRepository(JobDbContext dbContext) : IProcessedMessageRepository
{
    public async Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken)
    {
        return await dbContext.ProcessedMessages
            .AnyAsync(p => p.MessageId == messageId, cancellationToken);
    }

    public async Task AddAsync(string messageId, CancellationToken cancellationToken)
    {
        await dbContext.ProcessedMessages.AddAsync(
            new ProcessedMessage { MessageId = messageId, ProcessedAt = DateTimeOffset.UtcNow },
            cancellationToken);
    }
}
