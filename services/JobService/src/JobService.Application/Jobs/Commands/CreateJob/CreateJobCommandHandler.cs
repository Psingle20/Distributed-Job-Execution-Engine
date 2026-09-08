using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using JobEngine.Contracts.IntegrationEvents;
using JobService.Application.Abstractions;
using JobService.Domain.Jobs;

namespace JobService.Application.Jobs.Commands.CreateJob;

internal sealed class CreateJobCommandHandler(
    IJobRepository jobRepository,
    IOutboxEventPublisher outbox,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateJobCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateJobCommand command, CancellationToken cancellationToken)
    {
        var job = Job.Create(command.Type, command.PayloadJson, command.MaxAttempts, dateTimeProvider.UtcNow);

        await jobRepository.AddAsync(job, cancellationToken);

        outbox.Enqueue("jobs.created", new JobCreatedIntegrationEvent(
            job.Id, job.Type, job.PayloadJson, job.MaxAttempts, job.CreatedAt));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(job.Id);
    }
}
