using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobDetails;

namespace JobScheduler.Dal.UnitTests;

public class JobPayloadsTests
{
    [Fact]
    public void ToSnapshotPayload_MapsViewToPayload()
    {
        var view = new JobDetailsView
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "nightly-report",
            Status = JobStatus.Active,
            ConcurrencyMode = ConcurrencyMode.Concurrent,
            Version = 4,
            CreatedAt = new DateTime(2026, 1, 1),
            UpdatedAt = new DateTime(2026, 1, 2),
            ScheduleType = ScheduleType.Recurring,
            StartAt = new DateTime(2099, 1, 1, 2, 0, 0),
            RepeatStepSize = 1,
            RepeatStepUnit = RepeatStepUnit.Day,
            Endpoint = "https://example.com/hooks/nightly-report",
            HttpMethod = LaunchHttpMethod.POST,
            Headers = """{"Content-Type":"application/json"}""",
            Payload = null,
        };

        var payload = JobPayloads.ToSnapshotPayload(view);

        Assert.Equal(view.Id, payload.JobId);
        Assert.Equal(view.UserId, payload.UserId);
        Assert.Equal(4, payload.Version);
        Assert.Equal(JobStatus.Active, payload.Status);
        Assert.Equal(ConcurrencyMode.Concurrent, payload.ConcurrencyMode);
        Assert.Equal(ScheduleType.Recurring, payload.ScheduleType);
        Assert.Equal(view.StartAt, payload.StartAt);
        Assert.Equal(1, payload.RepeatStepSize);
        Assert.Equal(RepeatStepUnit.Day, payload.RepeatStepUnit);
        Assert.Equal(view.Endpoint, payload.LaunchDetails.Endpoint);
        Assert.Equal(LaunchHttpMethod.POST, payload.LaunchDetails.HttpMethod);
        Assert.Equal(view.Headers, payload.LaunchDetails.Headers);
        Assert.Null(payload.LaunchDetails.Payload);
    }
}
