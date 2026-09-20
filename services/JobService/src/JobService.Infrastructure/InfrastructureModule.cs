using CleanArchitecture.BuildingBlocks;
using Dapr.EntityFrameworkCore.Outbox;
using Dapr.EntityFrameworkCore.Outbox.DependencyInjection;
using JobService.Application.Abstractions;
using JobService.Infrastructure.BackgroundServices;
using JobService.Infrastructure.Chaos;
using JobService.Infrastructure.Persistence;
using JobService.Infrastructure.Persistence.Repositories;
using JobService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobService.Infrastructure;

public sealed class InfrastructureModule : IDependencyModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("JobServiceDb")
            ?? "Host=localhost;Port=5432;Database=jobservice_db;Username=postgres;Password=postgres";

        services.AddSingleton<IChaosState, InMemoryChaosState>();
        services.AddScoped<ChaosEfInterceptor>();

        services.AddDbContext<JobDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<DaprOutboxSaveChangesInterceptor>(),
                sp.GetRequiredService<ChaosEfInterceptor>());
        });

        services.AddDaprOutbox<JobDbContext>()
            .AddDefaultDispatcher()
            .AddPostgreSqlClaimStrategy();

        services.Decorate<IOutboxDispatcher, ChaosOutboxDispatcherDecorator>();

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<IOutboxEventPublisher, OutboxEventPublisher>();

        services.AddHostedService<LeaseRecoveryBackgroundService>();
        services.AddHostedService<DueJobDispatcherService>();
    }
}
