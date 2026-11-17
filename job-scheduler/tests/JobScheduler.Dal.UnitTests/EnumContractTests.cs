using JobScheduler.Dal.Contracts;

namespace JobScheduler.Dal.UnitTests;

// Expected lists are copied from the CHECK constraints in db/*/001_schema.sql. Enum values are persisted
// as their names, so a rename or a new member without a matching schema change must fail here.
public class EnumContractTests
{
    [Fact]
    public void JobStatus_MatchesSqlCheck() =>
        Assert.Equal(["Active", "Inactive", "Deleted"], Enum.GetNames<JobStatus>());

    [Fact]
    public void ConcurrencyMode_MatchesSqlCheck() =>
        Assert.Equal(["Singleton", "Concurrent"], Enum.GetNames<ConcurrencyMode>());

    [Fact]
    public void ScheduleType_MatchesSqlCheck() =>
        Assert.Equal(["OneTime", "Recurring"], Enum.GetNames<ScheduleType>());

    [Fact]
    public void RepeatStepUnit_MatchesSqlCheck() =>
        Assert.Equal(["Minute", "Hour", "Day", "Week", "Month"], Enum.GetNames<RepeatStepUnit>());

    [Fact]
    public void LaunchHttpMethod_MatchesSqlCheck() =>
        Assert.Equal(["GET", "POST", "PUT", "PATCH", "DELETE"], Enum.GetNames<LaunchHttpMethod>());

    [Fact]
    public void OutboxEventType_MatchesSqlCheck() =>
        Assert.Equal(["JobCreated", "JobUpdated", "JobDeleted"], Enum.GetNames<OutboxEventType>());

    [Fact]
    public void JobRunStatus_MatchesSqlCheck() =>
        Assert.Equal(["Pending", "Running", "Succeeded", "Failed", "TimedOut", "LockFailed", "Cancelled"], Enum.GetNames<JobRunStatus>());

    [Fact]
    public void RunOutboxEventType_MatchesSqlCheck() =>
        Assert.Equal(["ExecuteJob", "NotifyFailure"], Enum.GetNames<RunOutboxEventType>());
}
