using System.Globalization;
using System.Text.Json;
using JobScheduler.Dal.Common;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobOrchestrator;

namespace JobScheduler.Dal.UnitTests;

public class JobSnapshotMapperTests
{
    private static readonly DateTime Now = D("2026-11-13T10:00:00");

    private static DateTime D(string value) => DateTime.Parse(value, CultureInfo.InvariantCulture);

    private static JobSnapshotPayload Payload(
        ScheduleType scheduleType,
        DateTime startAt,
        int? step = null,
        RepeatStepUnit? unit = null) => new()
    {
        JobId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Version = 3,
        Status = JobStatus.Active,
        ConcurrencyMode = ConcurrencyMode.Singleton,
        ScheduleType = scheduleType,
        StartAt = startAt,
        RepeatStepSize = step,
        RepeatStepUnit = unit,
        LaunchDetails = new LaunchDetailsPayload
        {
            Endpoint = "https://example.com/hooks/run",
            HttpMethod = LaunchHttpMethod.POST,
            Headers = """{"Content-Type":"application/json"}""",
            Payload = """{"format":"pdf"}""",
        },
    };

    [Fact]
    public void OneTime_KeepsStartAtEvenWhenInThePast()
    {
        var startAt = D("2026-01-01T02:00:00");

        var snapshot = JobSnapshotMapper.ToSnapshot(Payload(ScheduleType.OneTime, startAt), Now);

        Assert.Equal(startAt, snapshot.NextRunAt);
    }

    [Fact]
    public void Recurring_FutureStartAt_IsKept()
    {
        var startAt = D("2026-12-01T00:00:00");

        var snapshot = JobSnapshotMapper.ToSnapshot(Payload(ScheduleType.Recurring, startAt, 1, RepeatStepUnit.Day), Now);

        Assert.Equal(startAt, snapshot.NextRunAt);
    }

    [Fact]
    public void Recurring_PastStartAt_ReturnsNextOccurrenceAfterNow()
    {
        var snapshot = JobSnapshotMapper.ToSnapshot(
            Payload(ScheduleType.Recurring, D("2026-11-13T08:00:00"), 15, RepeatStepUnit.Minute), D("2026-11-13T10:20:00"));

        Assert.Equal(D("2026-11-13T10:30:00"), snapshot.NextRunAt);
    }

    [Fact]
    public void Recurring_StartAtExactlyNow_ReturnsFollowingOccurrence()
    {
        var snapshot = JobSnapshotMapper.ToSnapshot(Payload(ScheduleType.Recurring, Now, 1, RepeatStepUnit.Hour), Now);

        Assert.Equal(Now.AddHours(1), snapshot.NextRunAt);
    }

    [Fact]
    public void CopiesPayloadFieldsAndLeavesRunLockEmpty()
    {
        var payload = Payload(ScheduleType.Recurring, D("2026-12-01T00:00:00"), 2, RepeatStepUnit.Week);

        var snapshot = JobSnapshotMapper.ToSnapshot(payload, Now);

        Assert.Equal(payload.JobId, snapshot.JobId);
        Assert.Equal(payload.UserId, snapshot.UserId);
        Assert.Equal(payload.ConcurrencyMode, snapshot.ConcurrencyMode);
        Assert.Equal(payload.ScheduleType, snapshot.ScheduleType);
        Assert.Equal(2, snapshot.RepeatStepSize);
        Assert.Equal(RepeatStepUnit.Week, snapshot.RepeatStepUnit);
        Assert.Equal(payload.Status, snapshot.Status);
        Assert.Equal(3, snapshot.Version);
        Assert.Equal(Now, snapshot.CreatedAt);
        Assert.Null(snapshot.CurrentRunId);
        Assert.Null(snapshot.LockedUntil);
    }

    [Fact]
    public void LaunchDetails_IsCamelCaseJsonWithStringEnum()
    {
        var payload = Payload(ScheduleType.OneTime, D("2026-12-01T00:00:00"));

        var snapshot = JobSnapshotMapper.ToSnapshot(payload, Now);

        using var json = JsonDocument.Parse(snapshot.LaunchDetails);
        var root = json.RootElement;
        Assert.Equal("https://example.com/hooks/run", root.GetProperty("endpoint").GetString());
        Assert.Equal("POST", root.GetProperty("httpMethod").GetString());
        Assert.Equal("""{"format":"pdf"}""", root.GetProperty("payload").GetString());
        Assert.Equal(payload.LaunchDetails, JsonSerializer.Deserialize<LaunchDetailsPayload>(snapshot.LaunchDetails, Json.Default));
    }

    [Fact]
    public void NullLaunchDetails_Throws()
    {
        var payload = Payload(ScheduleType.OneTime, D("2026-12-01T00:00:00")) with { LaunchDetails = null! };

        Assert.Throws<ArgumentNullException>(() => JobSnapshotMapper.ToSnapshot(payload, Now));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(1, null)]
    [InlineData(null, RepeatStepUnit.Day)]
    [InlineData(0, RepeatStepUnit.Day)]
    public void Recurring_WithoutValidStep_Throws(int? step, RepeatStepUnit? unit)
    {
        var payload = Payload(ScheduleType.Recurring, D("2026-12-01T00:00:00"), step, unit);

        Assert.Throws<ArgumentException>(() => JobSnapshotMapper.ToSnapshot(payload, Now));
    }

    [Fact]
    public void LocalStartAt_Throws()
    {
        var startAt = DateTime.SpecifyKind(D("2026-12-01T00:00:00"), DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => JobSnapshotMapper.ToSnapshot(Payload(ScheduleType.OneTime, startAt), Now));
    }

    [Fact]
    public void LocalUtcNow_Throws()
    {
        var localNow = DateTime.SpecifyKind(Now, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => JobSnapshotMapper.ToSnapshot(Payload(ScheduleType.OneTime, D("2026-12-01T00:00:00")), localNow));
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    public void NonLocalKinds_AreAccepted(DateTimeKind kind)
    {
        var startAt = DateTime.SpecifyKind(D("2026-12-01T00:00:00"), kind);

        var snapshot = JobSnapshotMapper.ToSnapshot(Payload(ScheduleType.OneTime, startAt), DateTime.SpecifyKind(Now, kind));

        Assert.Equal(startAt, snapshot.NextRunAt);
    }
}
