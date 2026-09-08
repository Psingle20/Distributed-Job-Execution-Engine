using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerService.Application.Abstractions;
using WorkerService.Infrastructure.Chaos;

namespace WorkerService.Infrastructure.BackgroundServices;

internal sealed class WorkerHeartbeatHostedService(
    IServiceScopeFactory scopeFactory,
    IChaosState chaosState,
    ILogger<WorkerHeartbeatHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);
    private static readonly string WorkerId = $"worker-{Environment.MachineName}-{Environment.ProcessId}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Heartbeat service started for {WorkerId}, interval {Interval}s",
            WorkerId, HeartbeatInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (chaosState.IsActive("stop-heartbeat"))
                {
                    logger.LogWarning("[CHAOS] Heartbeat suppressed by stop-heartbeat policy");
                }
                else
                {
                    await SendHeartbeatsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during heartbeat cycle");
            }

            await Task.Delay(HeartbeatInterval, stoppingToken);
        }
    }

    private async Task SendHeartbeatsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IWorkerExecutionRepository executionRepo = scope.ServiceProvider.GetRequiredService<IWorkerExecutionRepository>();
        IJobServiceClient jobServiceClient = scope.ServiceProvider.GetRequiredService<IJobServiceClient>();

        IReadOnlyList<Domain.Workers.WorkerExecution> activeExecutions =
            await executionRepo.GetActiveAsync(cancellationToken);

        if (activeExecutions.Count == 0)
        {
            return;
        }

        logger.LogDebug("Sending heartbeats for {Count} active executions", activeExecutions.Count);

        foreach (Domain.Workers.WorkerExecution execution in activeExecutions)
        {
            CleanArchitecture.BuildingBlocks.Result result =
                await jobServiceClient.HeartbeatAsync(execution.JobId, WorkerId, cancellationToken);

            if (result.IsFailure)
            {
                logger.LogWarning("Heartbeat failed for job {JobId}: {Error}",
                    execution.JobId, result.Error.Description);
            }
        }
    }
}
