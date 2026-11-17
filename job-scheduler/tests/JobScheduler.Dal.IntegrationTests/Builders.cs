using System.Text.Json;
using JobScheduler.Dal.Common;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobDetails;
using JobScheduler.Dal.JobOrchestrator;

namespace JobScheduler.Dal.IntegrationTests;

public static class Builders
{
    public static string UniqueName() => $"it-{Guid.NewGuid():N}";

    // A whole-minute instant in 2001-2002: far from the seed rows (2099) and from real "now", so a claim at this
    // time can only pick rows the test (or an earlier failed run) put in that window. Whole minutes survive DATETIME2(3).
    public static DateTime SyntheticNow() =>
        new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(Random.Shared.Next(1, 500_000));

    public static LaunchDetailsPayload LaunchDetails() => new()
    {
        Endpoint = "https://example.com/hooks/integration-test",
        HttpMethod = LaunchHttpMethod.POST,
        Headers = """{"Content-Type":"application/json"}""",
        Payload = """{"source":"integration-test"}""",
    };

    public static NewJob NewJob(
        Guid? userId = null,
        string? name = null,
        ConcurrencyMode concurrencyMode = ConcurrencyMode.Concurrent) => new()
    {
        UserId = userId ?? Guid.NewGuid(),
        Name = name ?? UniqueName(),
        Description = "created by integration tests",
        ConcurrencyMode = concurrencyMode,
        Schedule = new JobScheduleSpec
        {
            ScheduleType = ScheduleType.Recurring,
            StartAt = new DateTime(2099, 1, 1, 2, 0, 0, DateTimeKind.Utc),
            RepeatStepSize = 1,
            RepeatStepUnit = RepeatStepUnit.Day,
        },
        LaunchDetails = new LaunchDetailsSpec
        {
            Endpoint = "https://example.com/hooks/integration-test",
            HttpMethod = LaunchHttpMethod.POST,
            Headers = """{"Content-Type":"application/json"}""",
            Payload = """{"source":"integration-test"}""",
        },
    };

    public static UpdateJob UpdateOf(JobDetailsView job, int expectedVersion, string newName) => new()
    {
        JobId = job.Id,
        ExpectedVersion = expectedVersion,
        Name = newName,
        Description = job.Description,
        ConcurrencyMode = job.ConcurrencyMode,
        Schedule = new JobScheduleSpec
        {
            ScheduleType = job.ScheduleType,
            StartAt = job.StartAt,
            RepeatStepSize = job.RepeatStepSize,
            RepeatStepUnit = job.RepeatStepUnit,
        },
        LaunchDetails = new LaunchDetailsSpec
        {
            Endpoint = job.Endpoint,
            HttpMethod = job.HttpMethod,
            Headers = job.Headers,
            Payload = job.Payload,
        },
    };

    // Due exactly one second before utcNow, so a claim at utcNow picks it and ScheduledFor is predictable.
    public static JobScheduleSnapshot DueSnapshot(
        DateTime utcNow,
        ConcurrencyMode concurrencyMode = ConcurrencyMode.Concurrent,
        ScheduleType scheduleType = ScheduleType.Recurring,
        Guid? jobId = null) => Snapshot(
            utcNow.AddSeconds(-1), utcNow, concurrencyMode, scheduleType, jobId);

    // Far-future NextRunAt: never picked by any claim, for tests that only exercise upsert/read.
    public static JobScheduleSnapshot IdleSnapshot(Guid? jobId = null, int version = 1) =>
        Snapshot(new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateTime.UtcNow, ConcurrencyMode.Concurrent, ScheduleType.Recurring, jobId) with { Version = version };

    private static JobScheduleSnapshot Snapshot(
        DateTime nextRunAt, DateTime createdAt, ConcurrencyMode concurrencyMode, ScheduleType scheduleType, Guid? jobId) => new()
    {
        JobId = jobId ?? Ids.NewId(),
        UserId = Guid.NewGuid(),
        ConcurrencyMode = concurrencyMode,
        ScheduleType = scheduleType,
        RepeatStepSize = scheduleType == ScheduleType.Recurring ? 1 : null,
        RepeatStepUnit = scheduleType == ScheduleType.Recurring ? RepeatStepUnit.Minute : null,
        NextRunAt = nextRunAt,
        LaunchDetails = JsonSerializer.Serialize(LaunchDetails(), Json.Default),
        Status = JobStatus.Active,
        Version = 1,
        CreatedAt = TruncateToMilliseconds(createdAt),
    };

    private static DateTime TruncateToMilliseconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, value.Kind);
}
