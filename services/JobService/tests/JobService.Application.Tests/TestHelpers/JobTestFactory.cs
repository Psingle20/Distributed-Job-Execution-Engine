using JobService.Domain.Jobs;

namespace JobService.Application.Tests.TestHelpers;

internal static class JobTestFactory
{
    public static readonly DateTimeOffset Now = new(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);

    public static Job CreatePendingJob(int maxAttempts = 3) =>
        Job.Create("generate-report", """{"key":"value"}""", maxAttempts, Now);

    public static Job CreateRunningJob(string workerId = "worker-1", int maxAttempts = 3)
    {
        var job = CreatePendingJob(maxAttempts);
        job.Claim(workerId, Now, LeaseDuration);
        return job;
    }

    public static Job CreateCompletedJob(string workerId = "worker-1")
    {
        var job = CreateRunningJob(workerId);
        job.Complete(workerId, job.ExecutionId!.Value, Now.AddSeconds(5));
        return job;
    }

    public static Job CreateFailedJob(string workerId = "worker-1", int maxAttempts = 1)
    {
        var job = CreateRunningJob(workerId, maxAttempts);
        job.Fail(workerId, job.ExecutionId!.Value, Now.AddSeconds(5), "permanent error");
        return job;
    }

    public static Job CreateRetryingJob(string workerId = "worker-1", int maxAttempts = 3)
    {
        var job = CreateRunningJob(workerId, maxAttempts);
        job.Fail(workerId, job.ExecutionId!.Value, Now.AddSeconds(5), "transient error");
        return job;
    }

    public static Job CreateExpiredLeaseJob(string workerId = "worker-1", int maxAttempts = 3)
    {
        var job = CreatePendingJob(maxAttempts);
        job.Claim(workerId, Now.AddMinutes(-2), LeaseDuration);
        return job;
    }
}
