using CleanArchitecture.BuildingBlocks;
using JobService.Application.Abstractions;
using JobService.Application.Jobs.Commands.CancelJob;
using JobService.Application.Tests.TestHelpers;
using JobService.Domain.Jobs;
using Moq;
using Shouldly;

namespace JobService.Application.Tests.Jobs.Commands;

public sealed class CancelJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly MockDateTimeProvider _dateTimeProvider = new(JobTestFactory.Now);
    private readonly CancelJobCommandHandler _handler;

    public CancelJobCommandHandlerTests()
    {
        _handler = new CancelJobCommandHandler(
            _jobRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Should_cancel_pending_job()
    {
        var job = JobTestFactory.CreatePendingJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new CancelJobCommand(job.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Cancelled);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_cancel_running_job()
    {
        var job = JobTestFactory.CreateRunningJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new CancelJobCommand(job.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Cancelled);
    }

    [Fact]
    public async Task Should_fail_for_nonexistent_job()
    {
        var jobId = Guid.NewGuid();
        _jobRepository.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var result = await _handler.Handle(new CancelJobCommand(jobId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Should_fail_for_completed_job()
    {
        var job = JobTestFactory.CreateCompletedJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new CancelJobCommand(job.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_not_save_when_cancel_fails()
    {
        var job = JobTestFactory.CreateCompletedJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await _handler.Handle(new CancelJobCommand(job.Id), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
