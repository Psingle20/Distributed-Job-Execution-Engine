using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Abstractions;
using JobService.Application.Observability;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.ClaimJob;

internal sealed class ClaimJobCommandHandler(
    IJobRepository jobRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ClaimJobCommand, ClaimJobResult>
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);

    public async Task<Result<ClaimJobResult>> Handle(ClaimJobCommand command, CancellationToken cancellationToken)
    {
        Job? job = await jobRepository.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
        {
            return Result.Failure<ClaimJobResult>(JobErrors.NotFound(command.JobId));
        }

        Result result = job.Claim(command.WorkerId, dateTimeProvider.UtcNow, LeaseDuration);
        if (result.IsFailure)
        {
            JobServiceDiagnostics.ClaimConflicts.Add(1);
            return Result.Failure<ClaimJobResult>(result.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new ClaimJobResult(job.ExecutionId!.Value, job.AttemptCount, job.LeaseUntil!.Value));
    }
}
