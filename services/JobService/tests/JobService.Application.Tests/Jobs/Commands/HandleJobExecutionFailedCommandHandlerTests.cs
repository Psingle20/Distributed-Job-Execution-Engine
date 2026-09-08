using CleanArchitecture.BuildingBlocks;
using JobService.Application.Abstractions;
using JobService.Application.Jobs.Commands.HandleJobExecutionFailed;
using JobService.Application.Tests.TestHelpers;
using JobService.Domain.Jobs;
using Moq;
using Shouldly;

namespace JobService.Application.Tests.Jobs.Commands;

public sealed class HandleJobExecutionFailedCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IProcessedMessageRepository> _processedMessages = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly HandleJobExecutionFailedCommandHandler _handler;

    public HandleJobExecutionFailedCommandHandlerTests()
    {
        _handler = new HandleJobExecutionFailedCommandHandler(
            _jobRepository.Object,
            _processedMessages.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Should_fail_job_with_retry_when_attempts_remain()
    {
        var job = JobTestFactory.CreateRunningJob("worker-1", maxAttempts: 3);
        var failedAt = JobTestFactory.Now.AddSeconds(5);
        _processedMessages.Setup(p => p.ExistsAsync("msg-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new HandleJobExecutionFailedCommand(
            "msg-1", job.Id, "worker-1", 1, "transient error", true, failedAt);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Retrying);
        job.LastError.ShouldBe("transient error");
    }

    [Fact]
    public async Task Should_fail_job_permanently_when_max_attempts_reached()
    {
        var job = JobTestFactory.CreateRunningJob("worker-1", maxAttempts: 1);
        var failedAt = JobTestFactory.Now.AddSeconds(5);
        _processedMessages.Setup(p => p.ExistsAsync("msg-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new HandleJobExecutionFailedCommand(
            "msg-2", job.Id, "worker-1", 1, "permanent error", false, failedAt);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Failed);
        job.FailedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_skip_duplicate_message()
    {
        _processedMessages.Setup(p => p.ExistsAsync("msg-dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new HandleJobExecutionFailedCommand(
            "msg-dup", Guid.NewGuid(), "worker-1", 1, "error", true, DateTimeOffset.UtcNow);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _jobRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_fail_for_nonexistent_job()
    {
        var jobId = Guid.NewGuid();
        _processedMessages.Setup(p => p.ExistsAsync("msg-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _jobRepository.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var command = new HandleJobExecutionFailedCommand(
            "msg-3", jobId, "worker-1", 1, "error", true, DateTimeOffset.UtcNow);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_record_processed_message_on_success()
    {
        var job = JobTestFactory.CreateRunningJob("worker-1", maxAttempts: 3);
        _processedMessages.Setup(p => p.ExistsAsync("msg-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new HandleJobExecutionFailedCommand(
            "msg-4", job.Id, "worker-1", 1, "error", true, JobTestFactory.Now.AddSeconds(5));
        await _handler.Handle(command, CancellationToken.None);

        _processedMessages.Verify(p => p.AddAsync("msg-4", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
