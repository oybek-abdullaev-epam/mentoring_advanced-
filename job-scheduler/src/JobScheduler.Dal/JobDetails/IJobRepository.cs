using JobScheduler.Dal.Connections;

namespace JobScheduler.Dal.JobDetails;

public interface IJobRepository
{
    // Returns the view built in memory (no re-read), so the caller never depends on replica lag.
    Task<JobDetailsView> CreateJobAsync(NewJob job, CancellationToken ct);

    // Eventual reads the Business Critical readable secondary (lag usually tens of ms, no upper bound); Strong reads
    // the primary. Edit forms must load with Strong: a stale Version from the replica causes a false conflict on update.
    Task<JobDetailsView?> GetJobAsync(Guid jobId, ReadConsistency consistency = ReadConsistency.Eventual, CancellationToken ct = default);

    // Always the readable secondary: a job created a moment ago may be missing.
    Task<IReadOnlyList<JobDetailsView>> ListJobsByUserAsync(Guid userId, CancellationToken ct = default);

    Task<int> UpdateJobAsync(UpdateJob cmd, CancellationToken ct);

    Task<int> SoftDeleteJobAsync(Guid jobId, int expectedVersion, CancellationToken ct);

    // Primary on purpose: replica lag would delay delivery and make "mark processed" race.
    Task<IReadOnlyList<JobOutboxMessage>> GetPendingOutboxAsync(int batchSize, CancellationToken ct);

    Task MarkOutboxProcessedAsync(Guid outboxId, CancellationToken ct);

    Task MarkOutboxFailedAttemptAsync(Guid outboxId, CancellationToken ct);
}
