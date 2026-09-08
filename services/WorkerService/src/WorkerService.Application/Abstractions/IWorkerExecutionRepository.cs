using WorkerService.Domain.Workers;

namespace WorkerService.Application.Abstractions;

public interface IWorkerExecutionRepository
{
    Task<WorkerExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkerExecution?> GetByJobIdAndAttemptAsync(Guid jobId, int attemptNumber, CancellationToken cancellationToken);
    Task AddAsync(WorkerExecution execution, CancellationToken cancellationToken);
    Task<int> GetActiveCountAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkerExecution>> GetActiveAsync(CancellationToken cancellationToken);
}
