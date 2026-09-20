using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Commands.DispatchDueRetries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobService.Infrastructure.BackgroundServices;

internal sealed class DueJobDispatcherService(
    IServiceScopeFactory scopeFactory,
    ILogger<DueJobDispatcherService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Due-job dispatcher started, polling every {Interval}s", PollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                ICommandDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

                await dispatcher.Dispatch(new DispatchDueRetriesCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during due-job dispatch");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
