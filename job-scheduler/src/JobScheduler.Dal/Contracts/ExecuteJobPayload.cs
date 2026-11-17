namespace JobScheduler.Dal.Contracts;

public sealed record ExecuteJobPayload
{
    public required Guid RunId { get; init; }
    public required Guid JobId { get; init; }
    public required DateTime ScheduledFor { get; init; }
    public required DateTime Deadline { get; init; }
    public required LaunchDetailsPayload LaunchDetails { get; init; }
}
