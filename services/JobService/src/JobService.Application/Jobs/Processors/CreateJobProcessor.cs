using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using FluentValidation;
using FluentValidation.Results;
using JobService.Application.Jobs.Commands.CreateJob;
using JobService.Application.Jobs.Dtos;

namespace JobService.Application.Jobs.Processors;

internal sealed class CreateJobProcessor(
    IValidator<CreateJobDto> validator,
    ICommandDispatcher commandDispatcher)
    : IProcessor<CreateJobDto, Guid>
{
    public async Task<Result<Guid>> Process(CreateJobDto dto, CancellationToken cancellationToken)
    {
        ValidationResult validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<Guid>(ValidationFailureMapper.ToValidationError(validation));
        }

        var command = new CreateJobCommand(dto.Type, dto.PayloadJson, dto.MaxAttempts);
        return await commandDispatcher.Dispatch(command, cancellationToken);
    }
}
