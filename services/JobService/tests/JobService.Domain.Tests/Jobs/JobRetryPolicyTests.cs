using JobService.Domain.Jobs;
using Shouldly;

namespace JobService.Domain.Tests.Jobs;

public sealed class JobRetryPolicyTests
{
    [Fact]
    public void First_attempt_should_be_approximately_one_second()
    {
        var delay = JobRetryPolicy.CalculateDelay(1);

        delay.TotalSeconds.ShouldBeGreaterThanOrEqualTo(1.0);
        delay.TotalSeconds.ShouldBeLessThan(1.15);
    }

    [Fact]
    public void Second_attempt_should_be_approximately_two_seconds()
    {
        var delay = JobRetryPolicy.CalculateDelay(2);

        delay.TotalSeconds.ShouldBeGreaterThanOrEqualTo(2.0);
        delay.TotalSeconds.ShouldBeLessThan(2.25);
    }

    [Fact]
    public void Fourth_attempt_should_be_approximately_eight_seconds()
    {
        var delay = JobRetryPolicy.CalculateDelay(4);

        delay.TotalSeconds.ShouldBeGreaterThanOrEqualTo(8.0);
        delay.TotalSeconds.ShouldBeLessThan(9.0);
    }

    [Fact]
    public void High_attempt_should_cap_at_sixty_seconds()
    {
        var delay = JobRetryPolicy.CalculateDelay(10);

        delay.TotalSeconds.ShouldBeGreaterThanOrEqualTo(60.0);
        delay.TotalSeconds.ShouldBeLessThan(66.5);
    }
}
