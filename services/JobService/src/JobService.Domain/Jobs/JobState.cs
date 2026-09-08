namespace JobService.Domain.Jobs;

public enum JobState
{
    Pending,
    Running,
    Retrying,
    Completed,
    Failed,
    Cancelled
}
