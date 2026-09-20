namespace WorkerService.Infrastructure.Chaos;

public sealed record ChaosPolicy(
    string Name,
    ChaosType Type,
    Dictionary<string, string> Config);

public enum ChaosType
{
    CrashAfterClaim,
    CrashAfterEffect,
    FailNextJob,
    FailJobType,
    DelayExecution,
    StopHeartbeat
}
