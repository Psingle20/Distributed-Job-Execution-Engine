using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Commands.RecoverExpiredLeases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobService.Infrastructure.BackgroundServices;

internal sealed class LeaseRecoveryBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<LeaseRecoveryBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Lease recovery service started, scanning every {Interval}s", ScanInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                ICommandDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

                await dispatcher.Dispatch(new RecoverExpiredLeasesCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during lease recovery scan");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }
}
