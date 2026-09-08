using System.Security.Cryptography;

namespace JobService.Domain.Jobs;

public static class JobRetryPolicy
{
    private const int MaxDelaySeconds = 60;

    public static TimeSpan CalculateDelay(int attemptNumber)
    {
        double baseDelay = Math.Pow(2, attemptNumber - 1);
        double cappedDelay = Math.Min(baseDelay, MaxDelaySeconds);
        double jitter = RandomNumberGenerator.GetInt32(0, (int)(cappedDelay * 100)) / 1000.0;
        return TimeSpan.FromSeconds(cappedDelay + jitter);
    }
}
