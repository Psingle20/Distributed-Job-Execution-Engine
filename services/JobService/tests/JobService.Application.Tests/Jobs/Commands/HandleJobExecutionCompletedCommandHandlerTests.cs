using CleanArchitecture.BuildingBlocks;
using JobService.Application.Abstractions;
using JobService.Application.Jobs.Commands.HandleJobExecutionCompleted;
using JobService.Application.Tests.TestHelpers;
using JobService.Domain.Jobs;
using Moq;
using Shouldly;

namespace JobService.Application.Tests.Jobs.Commands;

public sealed class HandleJobExecutionCompletedCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IProcessedMessageRepository> _processedMessages = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly HandleJobExecutionCompletedCommandHandler _handler;

    public HandleJobExecutionCompletedCommandHandlerTests()
    {
        _handler = new HandleJobExecutionCompletedCommandHandler(
            _jobRepository.Object,
            _processedMessages.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Should_complete_running_job()
    {
        var job = JobTestFactory.CreateRunningJob("worker-1");
        var completedAt = JobTestFactory.Now.AddSeconds(5);
        _processedMessages.Setup(p => p.ExistsAsync("msg-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new HandleJobExecutionCompletedCommand("msg-1", job.Id, "worker-1", 1, null, completedAt);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Completed);
        job.CompletedAt.ShouldBe(completedAt);
    }

    [Fact]
    public async Task Should_skip_duplicate_message()
    {
        _processedMessages.Setup(p => p.ExistsAsync("msg-dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new HandleJobExecutionCompletedCommand(
            "msg-dup", Guid.NewGuid(), "worker-1", 1, null, DateTimeOffset.UtcNow);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _jobRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_record_processed_message()
    {
        var job = JobTestFactory.CreateRunningJob("worker-1");
        _processedMessages.Setup(p => p.ExistsAsync("msg-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new HandleJobExecutionCompletedCommand(
            "msg-2", job.Id, "worker-1", 1, null, JobTestFactory.Now.AddSeconds(5));
        await _handler.Handle(command, CancellationToken.None);

        _processedMessages.Verify(p => p.AddAsync("msg-2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_fail_for_nonexistent_job()
    {
        var jobId = Guid.NewGuid();
        _processedMessages.Setup(p => p.ExistsAsync("msg-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _jobRepository.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var command = new HandleJobExecutionCompletedCommand(
            "msg-3", jobId, "worker-1", 1, null, DateTimeOffset.UtcNow);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Should_fail_for_wrong_worker()
    {
        var job = JobTestFactory.CreateRunningJob("worker-1");
        _processedMessages.Setup(p => p.ExistsAsync("msg-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new HandleJobExecutionCompletedCommand(
            "msg-4", job.Id, "wrong-worker", 1, null, JobTestFactory.Now.AddSeconds(5));
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(JobErrors.NotLeaseOwner);
    }
}
