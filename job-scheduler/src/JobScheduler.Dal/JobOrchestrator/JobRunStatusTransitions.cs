using JobScheduler.Dal.Contracts;

namespace JobScheduler.Dal.JobOrchestrator;

public static class JobRunStatusTransitions
{
    public static bool IsAllowed(JobRunStatus from, JobRunStatus to) => from switch
    {
        JobRunStatus.Pending => to is JobRunStatus.Running or JobRunStatus.Cancelled or JobRunStatus.TimedOut,
        JobRunStatus.Running => to is JobRunStatus.Succeeded or JobRunStatus.Failed or JobRunStatus.TimedOut or JobRunStatus.Cancelled,
        _ => false,
    };

    // LockFailed is terminal from birth: the claimer inserts it directly and nothing ever transitions out of it.
    public static bool IsTerminal(JobRunStatus status) =>
        status is JobRunStatus.Succeeded or JobRunStatus.Failed or JobRunStatus.TimedOut or JobRunStatus.LockFailed or JobRunStatus.Cancelled;
}
