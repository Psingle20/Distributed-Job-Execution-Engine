using CleanArchitecture.BuildingBlocks;
using JobService.Application.Abstractions;
using JobService.Application.Jobs.Commands.HeartbeatJob;
using JobService.Application.Tests.TestHelpers;
using JobService.Domain.Jobs;
using Moq;
using Shouldly;

namespace JobService.Application.Tests.Jobs.Commands;

public sealed class HeartbeatJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly MockDateTimeProvider _dateTimeProvider = new(JobTestFactory.Now.AddSeconds(10));
    private readonly HeartbeatJobCommandHandler _handler;

    public HeartbeatJobCommandHandlerTests()
    {
        _handler = new HeartbeatJobCommandHandler(
            _jobRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Should_extend_lease_for_owner()
    {
        var job = JobTestFactory.CreateRunningJob("worker-1");
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(
            new HeartbeatJobCommand(job.Id, "worker-1", job.ExecutionId!.Value), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.LeaseUntil.ShouldBe(_dateTimeProvider.UtcNow + TimeSpan.FromSeconds(30));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_fail_for_non_owner()
    {
        var job = JobTestFactory.CreateRunningJob("worker-1");
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(
            new HeartbeatJobCommand(job.Id, "worker-2", job.ExecutionId!.Value), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(JobErrors.NotLeaseOwner);
    }

    [Fact]
    public async Task Should_fail_for_nonexistent_job()
    {
        var jobId = Guid.NewGuid();
        _jobRepository.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var result = await _handler.Handle(
            new HeartbeatJobCommand(jobId, "worker-1", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Should_fail_for_pending_job()
    {
        var job = JobTestFactory.CreatePendingJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(
            new HeartbeatJobCommand(job.Id, "worker-1", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
