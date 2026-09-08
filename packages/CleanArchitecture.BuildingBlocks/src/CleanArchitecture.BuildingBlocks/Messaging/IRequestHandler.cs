namespace CleanArchitecture.BuildingBlocks.Messaging;

public interface IRequestHandler
{
    Task<Result> Handle<TDto>(TDto dto, CancellationToken cancellationToken)
        where TDto : IDto;

    Task<Result<TResult>> Handle<TDto, TResult>(TDto dto, CancellationToken cancellationToken)
        where TDto : IDto<TResult>;
}
