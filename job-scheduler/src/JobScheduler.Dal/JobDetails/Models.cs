using JobScheduler.Dal.Contracts;

namespace JobScheduler.Dal.JobDetails;

public sealed record JobScheduleSpec
{
    public required ScheduleType ScheduleType { get; init; }
    public required DateTime StartAt { get; init; }
    public int? RepeatStepSize { get; init; }
    public RepeatStepUnit? RepeatStepUnit { get; init; }
}

public sealed record LaunchDetailsSpec
{
    public required string Endpoint { get; init; }
    public required LaunchHttpMethod HttpMethod { get; init; }
    public string? Headers { get; init; }
    public string? Payload { get; init; }
}

public sealed record NewJob
{
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required ConcurrencyMode ConcurrencyMode { get; init; }
    public required JobScheduleSpec Schedule { get; init; }
    public required LaunchDetailsSpec LaunchDetails { get; init; }
}

public sealed record UpdateJob
{
    public required Guid JobId { get; init; }
    public required int ExpectedVersion { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required ConcurrencyMode ConcurrencyMode { get; init; }
    public required JobScheduleSpec Schedule { get; init; }
    public required LaunchDetailsSpec LaunchDetails { get; init; }
}

// Flat on purpose (Job + JobSchedule + JobLaunchDetails columns): a single Dapper query maps it without multi-mapping.
public sealed record JobDetailsView
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required JobStatus Status { get; init; }
    public required ConcurrencyMode ConcurrencyMode { get; init; }
    public required int Version { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }

    public required ScheduleType ScheduleType { get; init; }
    public required DateTime StartAt { get; init; }
    public int? RepeatStepSize { get; init; }
    public RepeatStepUnit? RepeatStepUnit { get; init; }

    public required string Endpoint { get; init; }
    public required LaunchHttpMethod HttpMethod { get; init; }
    public string? Headers { get; init; }
    public string? Payload { get; init; }
}

public sealed record JobOutboxMessage
{
    public required Guid Id { get; init; }
    public required Guid JobId { get; init; }
    public required OutboxEventType EventType { get; init; }
    public required int JobVersion { get; init; }
    public required string Payload { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public required int Attempts { get; init; }
}

public static class JobPayloads
{
    public static JobSnapshotPayload ToSnapshotPayload(JobDetailsView v) => new()
    {
        JobId = v.Id,
        UserId = v.UserId,
        Version = v.Version,
        Status = v.Status,
        ConcurrencyMode = v.ConcurrencyMode,
        ScheduleType = v.ScheduleType,
        StartAt = v.StartAt,
        RepeatStepSize = v.RepeatStepSize,
        RepeatStepUnit = v.RepeatStepUnit,
        LaunchDetails = new LaunchDetailsPayload
        {
            Endpoint = v.Endpoint,
            HttpMethod = v.HttpMethod,
            Headers = v.Headers,
            Payload = v.Payload,
        },
    };
}
