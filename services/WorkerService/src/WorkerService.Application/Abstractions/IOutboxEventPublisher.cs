namespace WorkerService.Application.Abstractions;

public interface IOutboxEventPublisher
{
    void Enqueue(string topic, object payload);
}
