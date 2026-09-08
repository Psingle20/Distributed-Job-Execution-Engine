using CleanArchitecture.BuildingBlocks;
using JobService.Domain.Jobs;
using JobService.Domain.Jobs.Events;
using Shouldly;

namespace JobService.Domain.Tests.Jobs;

public sealed class JobTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);

    private static Job CreateTestJob(int maxAttempts = 3) =>
        Job.Create("generate-report", """{"key":"value"}""", maxAttempts, Now);

    [Fact]
    public void Create_should_set_initial_state()
    {
        var job = CreateTestJob();

        job.Id.ShouldNotBe(Guid.Empty);
        job.Type.ShouldBe("generate-report");
        job.State.ShouldBe(JobState.Pending);
        job.AttemptCount.ShouldBe(0);
        job.MaxAttempts.ShouldBe(3);
        job.CreatedAt.ShouldBe(Now);
        job.WorkerId.ShouldBeNull();
        job.LeaseUntil.ShouldBeNull();
    }

    [Fact]
    public void Create_should_raise_domain_event()
    {
        var job = CreateTestJob();

        job.DomainEvents.ShouldContain(e => e is JobCreatedDomainEvent);
    }

    [Fact]
    public void Create_should_record_state_transition()
    {
        var job = CreateTestJob();

        job.StateTransitions.Count.ShouldBe(1);
        job.StateTransitions[0].FromState.ShouldBeNull();
        job.StateTransitions[0].ToState.ShouldBe(JobState.Pending);
    }

    [Fact]
    public void Claim_from_pending_should_succeed()
    {
        var job = CreateTestJob();
        job.ClearDomainEvents();

        var result = job.Claim("worker-1", Now, LeaseDuration);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Running);
        job.WorkerId.ShouldBe("worker-1");
        job.LeaseUntil.ShouldBe(Now + LeaseDuration);
        job.AttemptCount.ShouldBe(1);
        job.Executions.Count.ShouldBe(1);
        job.DomainEvents.ShouldContain(e => e is JobClaimedDomainEvent);
    }

    [Fact]
    public void Claim_from_retrying_should_succeed()
    {
        var job = CreateTestJob();
        job.Claim("worker-1", Now, LeaseDuration);
        job.Fail("worker-1", Now.AddSeconds(5), "transient error");

        var result = job.Claim("worker-2", Now.AddSeconds(10), LeaseDuration);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Running);
        job.WorkerId.ShouldBe("worker-2");
        job.AttemptCount.ShouldBe(2);
    }

    [Fact]
    public void Claim_from_completed_should_fail()
    {
        var job = CreateTestJob();
        job.Claim("worker-1", Now, LeaseDuration);
        job.Complete("worker-1", Now.AddSeconds(5));

        var result = job.Claim("worker-2", Now.AddSeconds(10), LeaseDuration);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(JobErrors.NotClaimable);
    }

    [Fact]
    public void Claim_when_max_attempts_reached_should_fail()
    {
        var job = CreateTestJob(maxAttempts: 1);
        job.Claim("worker-1", Now, LeaseDuration);
        job.Fail("worker-1", Now.AddSeconds(5), "error");

        var result = job.Claim("worker-2", Now.AddSeconds(100), LeaseDuration);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Heartbeat_by_owner_should_succeed()
    {
        var job = CreateTestJob();
        job.Claim("worker-1", Now, LeaseDuration);

        var result = job.Heartbeat("worker-1", Now.AddSeconds(10), LeaseDuration);

        result.IsSuccess.ShouldBeTrue();
        job.LeaseUntil.ShouldBe(Now.AddSeconds(10) + LeaseDuration);
        job.LastHeartbeatAt.ShouldBe(Now.AddSeconds(10));
    }

    [Fact]
    public void Heartbeat_by_non_owner_should_fail()
    {
        var job = CreateTestJob();
        job.Claim("worker-1", Now, LeaseDuration);

        var result = job.Heartbeat("worker-2", Now.AddSeconds(10), LeaseDuration);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(JobErrors.NotLeaseOwner);
    }

    [Fact]
    public void Complete_by_owner_should_succeed()
    {
        var job = CreateTestJob();
        job.Claim("worker-1", Now, LeaseDuration);
        job.ClearDomainEvents();

        var result = job.Complete("worker-1", Now.AddSeconds(5));

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Completed);
        job.CompletedAt.ShouldBe(Now.AddSeconds(5));
        job.WorkerId.ShouldBeNull();
        job.LeaseUntil.ShouldBeNull();
        job.DomainEvents.ShouldContain(e => e is JobCompletedDomainEvent);
    }

    [Fact]
    public void Complete_by_non_owner_should_fail()
    {
        var job = CreateTestJob();
        job.Claim("worker-1", Now, LeaseDuration);

        var result = job.Complete("worker-2", Now.AddSeconds(5));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(JobErrors.NotLeaseOwner);
    }

    [Fact]
    public void Fail_with_retries_remaining_should_schedule_retry()
    {
        var job = CreateTestJob(maxAttempts: 3);
        job.Claim("worker-1", Now, LeaseDuration);
        job.ClearDomainEvents();

        var result = job.Fail("worker-1", Now.AddSeconds(5), "transient error");

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Retrying);
        job.WorkerId.ShouldBeNull();
        job.LeaseUntil.ShouldBeNull();
        job.LastError.ShouldBe("transient error");
        job.NextRunAt.ShouldNotBeNull();
        job.DomainEvents.ShouldContain(e => e is JobRetryScheduledDomainEvent);
    }

    [Fact]
    public void Fail_at_max_attempts_should_terminal_fail()
    {
        var job = CreateTestJob(maxAttempts: 1);
        job.Claim("worker-1", Now, LeaseDuration);
        job.ClearDomainEvents();

        var result = job.Fail("worker-1", Now.AddSeconds(5), "permanent error");

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Failed);
        job.FailedAt.ShouldNotBeNull();
        job.DomainEvents.ShouldContain(e => e is JobFailedDomainEvent);
    }

    [Fact]
    public void Cancel_from_pending_should_succeed()
    {
        var job = CreateTestJob();
        job.ClearDomainEvents();

        var result = job.Cancel(Now);

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Cancelled);
        job.DomainEvents.ShouldContain(e => e is JobCancelledDomainEvent);
    }

    [Fact]
    public void Cancel_from_running_should_succeed()
    {
        var job = CreateTestJob();
        job.Claim("worker-1", Now, LeaseDuration);

        var result = job.Cancel(Now.AddSeconds(5));

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Cancelled);
    }

    [Fact]
    public void Cancel_from_completed_should_fail()
    {
        var job = CreateTestJob();
        job.Claim("worker-1", Now, LeaseDuration);
        job.Complete("worker-1", Now.AddSeconds(5));

        var result = job.Cancel(Now.AddSeconds(10));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(JobErrors.AlreadyTerminal);
    }

    [Fact]
    public void MarkLeaseExpired_with_retries_should_schedule_retry()
    {
        var job = CreateTestJob(maxAttempts: 3);
        job.Claim("worker-1", Now, LeaseDuration);
        job.ClearDomainEvents();
        var expiredAt = Now.AddSeconds(35);

        var result = job.MarkLeaseExpired(expiredAt, expiredAt.AddSeconds(2));

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Retrying);
        job.WorkerId.ShouldBeNull();
        job.DomainEvents.ShouldContain(e => e is JobLeaseExpiredDomainEvent);
        job.DomainEvents.ShouldContain(e => e is JobRetryScheduledDomainEvent);
    }

    [Fact]
    public void MarkLeaseExpired_at_max_attempts_should_fail_job()
    {
        var job = CreateTestJob(maxAttempts: 1);
        job.Claim("worker-1", Now, LeaseDuration);
        job.ClearDomainEvents();
        var expiredAt = Now.AddSeconds(35);

        var result = job.MarkLeaseExpired(expiredAt, expiredAt.AddSeconds(2));

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Failed);
        job.FailedAt.ShouldNotBeNull();
        job.DomainEvents.ShouldContain(e => e is JobFailedDomainEvent);
    }

    [Fact]
    public void RetryFromFailed_should_transition_to_retrying()
    {
        var job = CreateTestJob(maxAttempts: 1);
        job.Claim("worker-1", Now, LeaseDuration);
        job.Fail("worker-1", Now.AddSeconds(5), "error");
        job.ClearDomainEvents();

        var result = job.RetryFromFailed(Now.AddSeconds(60));

        result.IsSuccess.ShouldBeTrue();
        job.State.ShouldBe(JobState.Retrying);
        job.FailedAt.ShouldBeNull();
        job.DomainEvents.ShouldContain(e => e is JobRetryScheduledDomainEvent);
    }

    [Fact]
    public void RetryFromFailed_when_not_failed_should_error()
    {
        var job = CreateTestJob();

        var result = job.RetryFromFailed(Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(JobErrors.NotFailed);
    }

    [Fact]
    public void CanBeClaimed_pending_should_be_true()
    {
        var job = CreateTestJob();

        job.CanBeClaimed(Now).ShouldBeTrue();
    }

    [Fact]
    public void CanBeClaimed_retrying_past_next_run_should_be_true()
    {
        var job = CreateTestJob();
        job.Claim("worker-1", Now, LeaseDuration);
        job.Fail("worker-1", Now.AddSeconds(5), "error");

        job.CanBeClaimed(Now.AddMinutes(5)).ShouldBeTrue();
    }

    [Fact]
    public void CanBeClaimed_completed_should_be_false()
    {
        var job = CreateTestJob();
        job.Claim("worker-1", Now, LeaseDuration);
        job.Complete("worker-1", Now.AddSeconds(5));

        job.CanBeClaimed(Now.AddMinutes(5)).ShouldBeFalse();
    }
}
