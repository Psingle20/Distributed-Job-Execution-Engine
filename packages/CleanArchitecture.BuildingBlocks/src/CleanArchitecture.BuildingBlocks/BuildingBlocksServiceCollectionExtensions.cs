using System.Reflection;
using CleanArchitecture.BuildingBlocks.Behaviors;
using CleanArchitecture.BuildingBlocks.Dispatchers;
using CleanArchitecture.BuildingBlocks.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.BuildingBlocks;

public static class BuildingBlocksServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocks(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        services.AddScoped<IRequestHandler, RequestHandler>();

        services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);

        return services;
    }

    public static IServiceCollection AddLoggingDecorator(this IServiceCollection services)
    {
        TryDecorate(services, typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
        TryDecorate(services, typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));
        TryDecorate(services, typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));
        return services;
    }

    public static IServiceCollection AddValidationDecorator(this IServiceCollection services)
    {
        TryDecorate(services, typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
        TryDecorate(services, typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandBaseHandler<>));
        return services;
    }

    private static void TryDecorate(IServiceCollection services, Type serviceType, Type decoratorType)
    {
        if (services.Any(s => s.ServiceType.IsGenericType && s.ServiceType.GetGenericTypeDefinition() == serviceType)
            || services.Any(s => s.ServiceType == serviceType))
        {
            services.Decorate(serviceType, decoratorType);
        }
    }
}
