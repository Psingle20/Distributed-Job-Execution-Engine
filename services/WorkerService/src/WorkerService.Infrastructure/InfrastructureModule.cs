using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using Dapr.EntityFrameworkCore.Outbox;
using Dapr.EntityFrameworkCore.Outbox.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkerService.Application.Abstractions;
using WorkerService.Application.Jobs.Handlers;
using WorkerService.Infrastructure.BackgroundServices;
using WorkerService.Infrastructure.Chaos;
using WorkerService.Infrastructure.Persistence;
using WorkerService.Infrastructure.Persistence.Repositories;
using WorkerService.Infrastructure.Services;

namespace WorkerService.Infrastructure;

public sealed class InfrastructureModule : IDependencyModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IChaosState, InMemoryChaosState>();

        services.AddDbContext<WorkerDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("WorkerDb"));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(sp.GetRequiredService<DaprOutboxSaveChangesInterceptor>());
        });

        services.AddDaprOutbox<WorkerDbContext>()
            .AddDefaultDispatcher()
            .AddPostgreSqlClaimStrategy();

        services.AddScoped<IWorkerExecutionRepository, WorkerExecutionRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddSingleton<IJobHandler, GenerateReportHandler>();
        services.AddSingleton<IJobHandler, DataExportHandler>();
        services.AddSingleton<IJobHandler, AlwaysFailHandler>();
        services.AddSingleton<IJobHandler, FailNTimesThenSucceedHandler>();
        services.AddSingleton<IJobHandler, SlowJobHandler>();
        services.AddSingleton<IJobHandlerRegistry, JobHandlerRegistry>();

        services.AddScoped<IJobServiceClient, JobServiceClient>();
        services.AddScoped<IOutboxEventPublisher, OutboxEventPublisher>();

        services.AddHostedService<WorkerHeartbeatHostedService>();

        services.Decorate(typeof(ICommandHandler<>), typeof(ChaosCommandDecorator<>));
    }
}
