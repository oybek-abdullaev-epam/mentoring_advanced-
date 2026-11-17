namespace JobScheduler.Dal.Contracts;

public sealed record JobSnapshotPayload
{
    public required Guid JobId { get; init; }
    public required Guid UserId { get; init; }
    public required int Version { get; init; }
    public required JobStatus Status { get; init; }
    public required ConcurrencyMode ConcurrencyMode { get; init; }
    public required ScheduleType ScheduleType { get; init; }
    public required DateTime StartAt { get; init; }
    public int? RepeatStepSize { get; init; }
    public RepeatStepUnit? RepeatStepUnit { get; init; }
    public required LaunchDetailsPayload LaunchDetails { get; init; }
}
