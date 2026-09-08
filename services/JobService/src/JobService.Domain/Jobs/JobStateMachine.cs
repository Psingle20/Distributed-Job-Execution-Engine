namespace JobService.Domain.Jobs;

public static class JobStateMachine
{
    private static readonly Dictionary<JobState, HashSet<JobState>> AllowedTransitions = new()
    {
        [JobState.Pending] = [JobState.Running, JobState.Cancelled],
        [JobState.Running] = [JobState.Completed, JobState.Retrying, JobState.Failed, JobState.Cancelled],
        [JobState.Retrying] = [JobState.Running, JobState.Failed, JobState.Cancelled],
        [JobState.Failed] = [JobState.Retrying],
        [JobState.Completed] = [],
        [JobState.Cancelled] = []
    };

    public static bool CanTransition(JobState from, JobState to) =>
        AllowedTransitions.TryGetValue(from, out HashSet<JobState>? allowed) && allowed.Contains(to);

    public static bool IsTerminal(JobState state) =>
        state is JobState.Completed or JobState.Cancelled;
}
