using JobService.Domain.Jobs;
using Shouldly;

namespace JobService.Domain.Tests.Jobs;

public sealed class JobStateMachineTests
{
    [Theory]
    [InlineData(JobState.Pending, JobState.Running, true)]
    [InlineData(JobState.Pending, JobState.Cancelled, true)]
    [InlineData(JobState.Running, JobState.Completed, true)]
    [InlineData(JobState.Running, JobState.Retrying, true)]
    [InlineData(JobState.Running, JobState.Failed, true)]
    [InlineData(JobState.Running, JobState.Cancelled, true)]
    [InlineData(JobState.Retrying, JobState.Running, true)]
    [InlineData(JobState.Retrying, JobState.Failed, true)]
    [InlineData(JobState.Retrying, JobState.Cancelled, true)]
    [InlineData(JobState.Failed, JobState.Retrying, true)]
    [InlineData(JobState.Completed, JobState.Running, false)]
    [InlineData(JobState.Completed, JobState.Failed, false)]
    [InlineData(JobState.Cancelled, JobState.Running, false)]
    [InlineData(JobState.Cancelled, JobState.Retrying, false)]
    [InlineData(JobState.Pending, JobState.Completed, false)]
    [InlineData(JobState.Pending, JobState.Failed, false)]
    [InlineData(JobState.Failed, JobState.Running, false)]
    [InlineData(JobState.Failed, JobState.Completed, false)]
    public void CanTransition_should_return_expected(JobState from, JobState to, bool expected)
    {
        JobStateMachine.CanTransition(from, to).ShouldBe(expected);
    }

    [Theory]
    [InlineData(JobState.Completed, true)]
    [InlineData(JobState.Cancelled, true)]
    [InlineData(JobState.Pending, false)]
    [InlineData(JobState.Running, false)]
    [InlineData(JobState.Retrying, false)]
    [InlineData(JobState.Failed, false)]
    public void IsTerminal_should_return_expected(JobState state, bool expected)
    {
        JobStateMachine.IsTerminal(state).ShouldBe(expected);
    }
}
