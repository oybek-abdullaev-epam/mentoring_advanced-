using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;

namespace JobScheduler.Dal.JobOrchestrator;

public interface IJobRunRepository
{
    // One primary transaction: picks due snapshots, advances them, takes Singleton locks and writes the runs
    // plus their ExecuteJob outbox rows. Parallel claimers skip each other's rows and never claim the same one.
    // After a transient error the transaction is re-run: the returned list may miss runs committed by an attempt
    // whose acknowledgement was lost. The run outbox is the record of what was claimed, not this list.
    Task<IReadOnlyList<ClaimedRun>> ClaimDueJobsAsync(DateTime utcNow, int batchSize, TimeSpan runLease, CancellationToken ct);

    // Compare-and-set: false means someone else moved the run to a status other than next. True also when the run
    // is already in next, so repeating the call is a success.
    Task<bool> TryUpdateStatusAsync(Guid runId, JobRunStatus expected, JobRunStatus next, DateTime utcNow, string? errorSummary, CancellationToken ct);

    // Only the run that owns the lock can clear it. Releasing a lock that runId no longer holds is a no-op.
    Task ReleaseLockAsync(Guid jobId, Guid runId, CancellationToken ct);

    Task SetServiceBusSequenceNumberAsync(Guid runId, long sequenceNumber, CancellationToken ct);

    // Primary on purpose, same reasoning as the job-details outbox.
    Task<IReadOnlyList<RunOutboxMessage>> GetPendingRunOutboxAsync(int batchSize, CancellationToken ct);

    Task MarkRunOutboxProcessedAsync(Guid outboxId, CancellationToken ct);

    Task<JobRunView?> GetRunAsync(Guid runId, ReadConsistency consistency = ReadConsistency.Eventual, CancellationToken ct = default);

    // Always the named reporting replica, so history queries never compete with the claimers on the primary.
    Task<IReadOnlyList<JobRunView>> GetRunHistoryAsync(Guid jobId, int take, CancellationToken ct);

    // Always the HA replica (ReadOnly), so it can be stale: a candidate list only. Callers confirm each run with
    // TryUpdateStatusAsync on the primary. Pending or Running runs whose Deadline has passed, oldest first.
    Task<IReadOnlyList<JobRunView>> GetOverdueRunsAsync(DateTime utcNow, int limit, CancellationToken ct);
}
