using FluentValidation;
using JobService.Application.Jobs.Dtos;

namespace JobService.Application.Jobs.Validators;

internal sealed class CreateJobDtoValidator : AbstractValidator<CreateJobDto>
{
    public CreateJobDtoValidator()
    {
        RuleFor(x => x.Type).NotEmpty().MaximumLength(128);
        RuleFor(x => x.PayloadJson).NotEmpty();
        RuleFor(x => x.MaxAttempts).GreaterThan(0).LessThanOrEqualTo(10);
    }
}
