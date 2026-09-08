using CleanArchitecture.BuildingBlocks;
using JobService.Application.Abstractions;
using JobService.Application.Jobs.Commands.RecoverExpiredLeases;
using JobService.Application.Tests.TestHelpers;
using JobService.Domain.Jobs;
using Moq;
using Shouldly;

namespace JobService.Application.Tests.Jobs.Commands;

public sealed class RecoverExpiredLeasesCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly MockDateTimeProvider _dateTimeProvider = new(JobTestFactory.Now.AddMinutes(5));
    private readonly RecoverExpiredLeasesCommandHandler _handler;

    public RecoverExpiredLeasesCommandHandlerTests()
    {
        _handler = new RecoverExpiredLeasesCommandHandler(
            _jobRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Should_recover_expired_lease_to_retrying()
    {
        var job = JobTestFactory.CreateExpiredLeaseJob("worker-1", maxAttempts: 3);
        _jobRepository.Setup(r => r.GetExpiredLeasesAsync(
                _dateTimeProvider.UtcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        var result = await _handler.Handle(new RecoverExpiredLeasesCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Retrying);
        job.WorkerId.ShouldBeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_recover_expired_lease_to_failed_when_max_attempts()
    {
        var job = JobTestFactory.CreateExpiredLeaseJob("worker-1", maxAttempts: 1);
        _jobRepository.Setup(r => r.GetExpiredLeasesAsync(
                _dateTimeProvider.UtcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        var result = await _handler.Handle(new RecoverExpiredLeasesCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Failed);
    }

    [Fact]
    public async Task Should_recover_multiple_expired_leases()
    {
        var job1 = JobTestFactory.CreateExpiredLeaseJob("worker-1", maxAttempts: 3);
        var job2 = JobTestFactory.CreateExpiredLeaseJob("worker-2", maxAttempts: 3);
        _jobRepository.Setup(r => r.GetExpiredLeasesAsync(
                _dateTimeProvider.UtcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job1, job2 });

        var result = await _handler.Handle(new RecoverExpiredLeasesCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        job1.State.ShouldBe(JobState.Retrying);
        job2.State.ShouldBe(JobState.Retrying);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_not_save_when_no_expired_leases()
    {
        _jobRepository.Setup(r => r.GetExpiredLeasesAsync(
                _dateTimeProvider.UtcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job>());

        var result = await _handler.Handle(new RecoverExpiredLeasesCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
