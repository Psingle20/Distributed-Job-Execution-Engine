using CleanArchitecture.BuildingBlocks;
using JobService.Application.Abstractions;
using JobService.Application.Jobs.Commands.RetryJob;
using JobService.Application.Tests.TestHelpers;
using JobService.Domain.Jobs;
using Moq;
using Shouldly;

namespace JobService.Application.Tests.Jobs.Commands;

public sealed class RetryJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly MockDateTimeProvider _dateTimeProvider = new(JobTestFactory.Now.AddMinutes(10));
    private readonly RetryJobCommandHandler _handler;

    public RetryJobCommandHandlerTests()
    {
        _handler = new RetryJobCommandHandler(
            _jobRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Should_retry_failed_job()
    {
        var job = JobTestFactory.CreateFailedJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new RetryJobCommand(job.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Retrying);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_fail_for_nonexistent_job()
    {
        var jobId = Guid.NewGuid();
        _jobRepository.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var result = await _handler.Handle(new RetryJobCommand(jobId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Should_fail_for_pending_job()
    {
        var job = JobTestFactory.CreatePendingJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new RetryJobCommand(job.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_fail_for_running_job()
    {
        var job = JobTestFactory.CreateRunningJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new RetryJobCommand(job.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
