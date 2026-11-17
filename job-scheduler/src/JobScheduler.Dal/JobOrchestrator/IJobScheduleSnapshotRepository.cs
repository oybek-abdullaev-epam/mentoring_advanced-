using JobScheduler.Dal.Connections;

namespace JobScheduler.Dal.JobOrchestrator;

public interface IJobScheduleSnapshotRepository
{
    // Version-guarded: a snapshot older than or equal to the stored one is ignored, so out-of-order or
    // duplicate outbox deliveries are harmless. Never touches CurrentRunId / LockedUntil.
    Task<SnapshotUpsertResult> UpsertAsync(JobScheduleSnapshot snapshot, CancellationToken ct);

    // Eventual reads the HA replica (lag); Strong reads the primary.
    Task<JobScheduleSnapshot?> GetAsync(Guid jobId, ReadConsistency consistency = ReadConsistency.Eventual, CancellationToken ct = default);
}
