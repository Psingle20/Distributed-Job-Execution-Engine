using Dapr.EntityFrameworkCore.Outbox;
using WorkerService.Application.Abstractions;
using WorkerService.Infrastructure.Persistence;

namespace WorkerService.Infrastructure.Services;

internal sealed class OutboxEventPublisher(WorkerDbContext dbContext) : IOutboxEventPublisher
{
    private const string PubSubName = "pubsub";

    public void Enqueue(string topic, object payload)
    {
        dbContext.EnqueueOutbox(PubSubName, topic, payload);
    }
}
