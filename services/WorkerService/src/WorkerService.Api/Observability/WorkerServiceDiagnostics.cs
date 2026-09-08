using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WorkerService.Api.Observability;

internal static class WorkerServiceDiagnostics
{
    public const string ServiceName = "WorkerService";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> JobsClaimed = Meter.CreateCounter<long>(
        "worker.jobs_claimed", "jobs", "Total jobs claimed by this worker");

    public static readonly Counter<long> JobsExecuted = Meter.CreateCounter<long>(
        "worker.jobs_executed", "jobs", "Total jobs executed (success + failure)");

    public static readonly Counter<long> JobsFailed = Meter.CreateCounter<long>(
        "worker.jobs_failed", "jobs", "Total job executions that failed");

    public static readonly Counter<long> ClaimConflicts = Meter.CreateCounter<long>(
        "worker.claim_conflicts", "claims", "Total claim attempts lost to another worker");

    public static readonly Counter<long> HeartbeatsSent = Meter.CreateCounter<long>(
        "worker.heartbeats_sent", "heartbeats", "Total heartbeats sent");

    public static readonly Counter<long> DuplicatesSkipped = Meter.CreateCounter<long>(
        "worker.duplicates_skipped", "messages", "Total duplicate messages detected and skipped");
}
