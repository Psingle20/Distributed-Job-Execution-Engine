using Dapr.EntityFrameworkCore.Outbox;
using JobService.Application.Abstractions;
using JobService.Infrastructure.Persistence;

namespace JobService.Infrastructure.Services;

internal sealed class OutboxEventPublisher(JobDbContext dbContext) : IOutboxEventPublisher
{
    private const string PubSubName = "pubsub";

    public void Enqueue(string topic, object payload)
    {
        dbContext.EnqueueOutbox(PubSubName, topic, payload);
    }
}
