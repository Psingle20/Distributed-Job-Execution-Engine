namespace CleanArchitecture.BuildingBlocks.Messaging;

public interface IProcessor<in TDto>
    where TDto : IDto
{
    Task<Result> Process(TDto dto, CancellationToken cancellationToken);
}

public interface IProcessor<in TDto, TResult>
    where TDto : IDto<TResult>
{
    Task<Result<TResult>> Process(TDto dto, CancellationToken cancellationToken);
}
