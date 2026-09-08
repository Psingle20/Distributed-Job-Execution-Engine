using System.Collections.Concurrent;
using System.Reflection;
using CleanArchitecture.BuildingBlocks.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.BuildingBlocks.Dispatchers;

internal sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    private static readonly ConcurrentDictionary<(Type CommandType, Type ResultType), HandlerInvoker> Invokers = new();

    public Task<Result<TResult>> Dispatch<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        HandlerInvoker invoker = Invokers.GetOrAdd(
            (command.GetType(), typeof(TResult)),
            static key =>
            {
                Type handlerInterface = typeof(ICommandHandler<,>).MakeGenericType(key.CommandType, key.ResultType);
                MethodInfo handleMethod = handlerInterface.GetMethod(nameof(ICommandHandler<ICommand<object>, object>.Handle))!;
                return new HandlerInvoker(handlerInterface, handleMethod);
            });

        object handler = serviceProvider.GetRequiredService(invoker.HandlerInterface);
        var task = (Task<Result<TResult>>)invoker.HandleMethod.Invoke(handler, [command, cancellationToken])!;
        return task;
    }

    public Task<Result> Dispatch(ICommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        HandlerInvoker invoker = Invokers.GetOrAdd(
            (command.GetType(), typeof(void)),
            static key =>
            {
                Type handlerInterface = typeof(ICommandHandler<>).MakeGenericType(key.CommandType);
                MethodInfo handleMethod = handlerInterface.GetMethod(nameof(ICommandHandler<ICommand>.Handle))!;
                return new HandlerInvoker(handlerInterface, handleMethod);
            });

        object handler = serviceProvider.GetRequiredService(invoker.HandlerInterface);
        var task = (Task<Result>)invoker.HandleMethod.Invoke(handler, [command, cancellationToken])!;
        return task;
    }

    private sealed record HandlerInvoker(Type HandlerInterface, MethodInfo HandleMethod);
}
