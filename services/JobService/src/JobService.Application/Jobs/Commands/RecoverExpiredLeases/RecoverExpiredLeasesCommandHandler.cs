using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Abstractions;
using JobService.Application.Observability;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.RecoverExpiredLeases;

internal sealed class RecoverExpiredLeasesCommandHandler(
    IJobRepository jobRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RecoverExpiredLeasesCommand>
{
    public async Task<Result> Handle(RecoverExpiredLeasesCommand command, CancellationToken cancellationToken)
    {
        DateTimeOffset now = dateTimeProvider.UtcNow;
        IReadOnlyList<Job> expiredJobs = await jobRepository.GetExpiredLeasesAsync(now, cancellationToken);

        foreach (Job job in expiredJobs)
        {
            TimeSpan delay = JobRetryPolicy.CalculateDelay(job.AttemptCount);
            DateTimeOffset retryAt = now + delay;
            job.MarkLeaseExpired(now, retryAt);
        }

        if (expiredJobs.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            JobServiceDiagnostics.LeasesRecovered.Add(expiredJobs.Count);
        }

        return Result.Success();
    }
}
