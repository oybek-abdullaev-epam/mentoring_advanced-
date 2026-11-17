using JobScheduler.Dal.Contracts;

namespace JobScheduler.Dal.JobOrchestrator;

public sealed record JobScheduleSnapshot
{
    public required Guid JobId { get; init; }
    public required Guid UserId { get; init; }
    public required ConcurrencyMode ConcurrencyMode { get; init; }
    public required ScheduleType ScheduleType { get; init; }
    public int? RepeatStepSize { get; init; }
    public RepeatStepUnit? RepeatStepUnit { get; init; }
    public required DateTime NextRunAt { get; init; }

    // JSON of LaunchDetailsPayload.
    public required string LaunchDetails { get; init; }
    public required JobStatus Status { get; init; }
    public required int Version { get; init; }
    public required DateTime CreatedAt { get; init; }
    public Guid? CurrentRunId { get; init; }
    public DateTime? LockedUntil { get; init; }
}

public enum SnapshotUpsertResult
{
    Inserted,
    Updated,
    IgnoredStale,
}

public sealed record ClaimedRun
{
    public required Guid RunId { get; init; }
    public required Guid JobId { get; init; }
    public required DateTime ScheduledFor { get; init; }

    // Pending or LockFailed.
    public required JobRunStatus Status { get; init; }

    // Null for OneTime jobs: nothing is scheduled after the claimed run.
    public DateTime? NextRunAt { get; init; }
}

public sealed record JobRunView
{
    public required Guid Id { get; init; }
    public required Guid JobId { get; init; }
    public required DateTime ScheduledFor { get; init; }
    public required JobRunStatus Status { get; init; }
    public long? ServiceBusSequenceNumber { get; init; }
    public DateTime? Deadline { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
    public string? ErrorSummary { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public sealed record RunOutboxMessage
{
    public required Guid Id { get; init; }
    public required Guid RunId { get; init; }
    public required RunOutboxEventType EventType { get; init; }
    public required string Payload { get; init; }
    public required DateTime ScheduledEnqueueAt { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public required int Attempts { get; init; }
}
