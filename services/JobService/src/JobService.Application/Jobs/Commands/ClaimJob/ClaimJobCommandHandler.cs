using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.ClaimJob;

internal sealed class ClaimJobCommandHandler(
    IJobRepository jobRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ClaimJobCommand>
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);

    public async Task<Result> Handle(ClaimJobCommand command, CancellationToken cancellationToken)
    {
        Job? job = await jobRepository.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound(command.JobId));
        }

        Result result = job.Claim(command.WorkerId, dateTimeProvider.UtcNow, LeaseDuration);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
