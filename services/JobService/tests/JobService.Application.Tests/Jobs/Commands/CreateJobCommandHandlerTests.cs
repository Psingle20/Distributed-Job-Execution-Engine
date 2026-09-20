using CleanArchitecture.BuildingBlocks;
using JobService.Application.Abstractions;
using JobService.Application.Jobs.Commands.CreateJob;
using JobService.Application.Tests.TestHelpers;
using JobService.Domain.Jobs;
using Moq;
using Shouldly;

namespace JobService.Application.Tests.Jobs.Commands;

public sealed class CreateJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IOutboxEventPublisher> _outbox = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly MockDateTimeProvider _dateTimeProvider = new(JobTestFactory.Now);
    private readonly CreateJobCommandHandler _handler;

    public CreateJobCommandHandlerTests()
    {
        _handler = new CreateJobCommandHandler(
            _jobRepository.Object,
            _outbox.Object,
            _unitOfWork.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Should_create_job_and_return_id()
    {
        var command = new CreateJobCommand("generate-report", """{"key":"value"}""", 3);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Should_add_job_to_repository()
    {
        var command = new CreateJobCommand("generate-report", """{"key":"value"}""", 3);

        await _handler.Handle(command, CancellationToken.None);

        _jobRepository.Verify(
            r => r.AddAsync(It.Is<Job>(j => j.Type == "generate-report" && j.State == JobState.Pending),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_save_changes()
    {
        var command = new CreateJobCommand("generate-report", """{"key":"value"}""", 3);

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_use_current_time_from_provider()
    {
        var specificTime = new DateTimeOffset(2026, 12, 25, 0, 0, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow = specificTime;
        var command = new CreateJobCommand("test-type", "{}", 1);

        await _handler.Handle(command, CancellationToken.None);

        _jobRepository.Verify(
            r => r.AddAsync(It.Is<Job>(j => j.CreatedAt == specificTime), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
