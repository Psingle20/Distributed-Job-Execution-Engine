using System.Collections.Concurrent;
using System.Reflection;
using CleanArchitecture.BuildingBlocks.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.BuildingBlocks.Dispatchers;

internal sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    private static readonly ConcurrentDictionary<(Type QueryType, Type ResultType), HandlerInvoker> Invokers = new();

    public Task<Result<TResult>> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        HandlerInvoker invoker = Invokers.GetOrAdd(
            (query.GetType(), typeof(TResult)),
            static key =>
            {
                Type handlerInterface = typeof(IQueryHandler<,>).MakeGenericType(key.QueryType, key.ResultType);
                MethodInfo handleMethod = handlerInterface.GetMethod(nameof(IQueryHandler<IQuery<object>, object>.Handle))!;
                return new HandlerInvoker(handlerInterface, handleMethod);
            });

        object handler = serviceProvider.GetRequiredService(invoker.HandlerInterface);
        var task = (Task<Result<TResult>>)invoker.HandleMethod.Invoke(handler, [query, cancellationToken])!;
        return task;
    }

    private sealed record HandlerInvoker(Type HandlerInterface, MethodInfo HandleMethod);
}
