using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace JobService.Api.Observability;

internal static class JobServiceDiagnostics
{
    public const string ServiceName = "JobService";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> JobsCreated = Meter.CreateCounter<long>(
        "jobs.created", "jobs", "Total jobs created");

    public static readonly Counter<long> JobsCompleted = Meter.CreateCounter<long>(
        "jobs.completed", "jobs", "Total jobs completed");

    public static readonly Counter<long> JobsFailed = Meter.CreateCounter<long>(
        "jobs.failed", "jobs", "Total jobs that reached terminal failure");

    public static readonly Counter<long> JobsCancelled = Meter.CreateCounter<long>(
        "jobs.cancelled", "jobs", "Total jobs cancelled");

    public static readonly Counter<long> LeasesRecovered = Meter.CreateCounter<long>(
        "jobs.leases_recovered", "leases", "Total expired leases recovered");

    public static readonly Counter<long> ClaimConflicts = Meter.CreateCounter<long>(
        "jobs.claim_conflicts", "claims", "Total claim attempts that failed due to concurrency");
}
