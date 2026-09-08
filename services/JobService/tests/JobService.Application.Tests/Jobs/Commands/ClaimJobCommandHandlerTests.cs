using CleanArchitecture.BuildingBlocks;
using JobService.Application.Abstractions;
using JobService.Application.Jobs.Commands.ClaimJob;
using JobService.Application.Tests.TestHelpers;
using JobService.Domain.Jobs;
using Moq;
using Shouldly;

namespace JobService.Application.Tests.Jobs.Commands;

public sealed class ClaimJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly MockDateTimeProvider _dateTimeProvider = new(JobTestFactory.Now);
    private readonly ClaimJobCommandHandler _handler;

    public ClaimJobCommandHandlerTests()
    {
        _handler = new ClaimJobCommandHandler(
            _jobRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Should_claim_pending_job()
    {
        var job = JobTestFactory.CreatePendingJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(
            new ClaimJobCommand(job.Id, "worker-1"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Running);
        job.WorkerId.ShouldBe("worker-1");
        job.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_claim_retrying_job()
    {
        var job = JobTestFactory.CreateRetryingJob();
        _dateTimeProvider.UtcNow = JobTestFactory.Now.AddMinutes(5);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(
            new ClaimJobCommand(job.Id, "worker-2"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.WorkerId.ShouldBe("worker-2");
    }

    [Fact]
    public async Task Should_fail_for_nonexistent_job()
    {
        var jobId = Guid.NewGuid();
        _jobRepository.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var result = await _handler.Handle(
            new ClaimJobCommand(jobId, "worker-1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Should_fail_for_completed_job()
    {
        var job = JobTestFactory.CreateCompletedJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(
            new ClaimJobCommand(job.Id, "worker-2"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_save_on_successful_claim()
    {
        var job = JobTestFactory.CreatePendingJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await _handler.Handle(new ClaimJobCommand(job.Id, "worker-1"), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_not_save_on_failed_claim()
    {
        var job = JobTestFactory.CreateCompletedJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await _handler.Handle(new ClaimJobCommand(job.Id, "worker-1"), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
