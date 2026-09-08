using CleanArchitecture.BuildingBlocks.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.BuildingBlocks.Dispatchers;

internal sealed class RequestHandler(IServiceProvider serviceProvider) : IRequestHandler
{
    public Task<Result> Handle<TDto>(TDto dto, CancellationToken cancellationToken)
        where TDto : IDto
    {
        ArgumentNullException.ThrowIfNull(dto);
        IProcessor<TDto> processor = serviceProvider.GetRequiredService<IProcessor<TDto>>();
        return processor.Process(dto, cancellationToken);
    }

    public Task<Result<TResult>> Handle<TDto, TResult>(TDto dto, CancellationToken cancellationToken)
        where TDto : IDto<TResult>
    {
        ArgumentNullException.ThrowIfNull(dto);
        IProcessor<TDto, TResult> processor = serviceProvider.GetRequiredService<IProcessor<TDto, TResult>>();
        return processor.Process(dto, cancellationToken);
    }
}
