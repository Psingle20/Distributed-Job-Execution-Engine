using WorkerService.Application.Abstractions;

namespace WorkerService.Infrastructure.Services;

internal sealed class JobHandlerRegistry : IJobHandlerRegistry
{
    private readonly Dictionary<string, IJobHandler> _handlers;

    public JobHandlerRegistry(IEnumerable<IJobHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.JobType, StringComparer.OrdinalIgnoreCase);
    }

    public IJobHandler? GetHandler(string jobType)
    {
        _handlers.TryGetValue(jobType, out IJobHandler? handler);
        return handler;
    }
}
