namespace JobService.Infrastructure.Chaos;

public sealed record ChaosPolicy(
    string Name,
    ChaosType Type,
    Dictionary<string, string> Config);

public enum ChaosType
{
    CrashAfterCommit,
    PauseOutbox,
    DelayOutbox,
    ForcePublishFailure,
    FailCommand
}
