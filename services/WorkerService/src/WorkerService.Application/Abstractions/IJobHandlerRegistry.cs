namespace WorkerService.Application.Abstractions;

public interface IJobHandlerRegistry
{
    IJobHandler? GetHandler(string jobType);
}
