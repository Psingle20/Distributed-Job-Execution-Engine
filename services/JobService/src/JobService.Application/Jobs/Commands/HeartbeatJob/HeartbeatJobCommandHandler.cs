using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.HeartbeatJob;

internal sealed class HeartbeatJobCommandHandler(
    IJobRepository jobRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<HeartbeatJobCommand>
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);

    public async Task<Result> Handle(HeartbeatJobCommand command, CancellationToken cancellationToken)
    {
        Job? job = await jobRepository.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound(command.JobId));
        }

        Result result = job.Heartbeat(command.WorkerId, command.ExecutionId, dateTimeProvider.UtcNow, LeaseDuration);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
