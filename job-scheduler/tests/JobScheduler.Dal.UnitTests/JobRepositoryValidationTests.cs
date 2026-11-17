using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobDetails;
using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.UnitTests;

public class JobRepositoryValidationTests
{
    private sealed class ThrowingConnectionFactory : ISqlConnectionFactory<JobDetailsDb>
    {
        public Task<SqlConnection> OpenAsync(DbRole role, CancellationToken ct = default) =>
            throw new InvalidOperationException("should not open");

        public Task<T> ExecuteAsync<T>(DbRole role, Func<SqlConnection, int, CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            throw new InvalidOperationException("should not open");
    }

    private static readonly JobRepository Repository = new(new ThrowingConnectionFactory(), TimeProvider.System);

    private static readonly JobScheduleSpec OneTime = new() { ScheduleType = ScheduleType.OneTime, StartAt = new DateTime(2099, 1, 1) };

    private static readonly JobScheduleSpec Daily = new()
    {
        ScheduleType = ScheduleType.Recurring,
        StartAt = new DateTime(2099, 1, 1),
        RepeatStepSize = 1,
        RepeatStepUnit = RepeatStepUnit.Day,
    };

    private static readonly LaunchDetailsSpec Launch = new() { Endpoint = "https://example.com/hook", HttpMethod = LaunchHttpMethod.POST };

    private static NewJob Job(string? name = "job", JobScheduleSpec? schedule = null, LaunchDetailsSpec? launch = null) => new()
    {
        UserId = Guid.NewGuid(),
        Name = name!,
        ConcurrencyMode = ConcurrencyMode.Singleton,
        Schedule = schedule ?? Daily,
        LaunchDetails = launch ?? Launch,
    };

    public static TheoryData<string, NewJob> InvalidJobs => new()
    {
        { "null name", Job(name: null) },
        { "empty name", Job(name: "") },
        { "blank name", Job(name: "   ") },
        { "name over 200 chars", Job(name: new string('x', 201)) },
        { "recurring without step", Job(schedule: Daily with { RepeatStepSize = null }) },
        { "recurring with zero step", Job(schedule: Daily with { RepeatStepSize = 0 }) },
        { "recurring with negative step", Job(schedule: Daily with { RepeatStepSize = -5 }) },
        { "recurring without unit", Job(schedule: Daily with { RepeatStepUnit = null }) },
        { "one-time with step size", Job(schedule: OneTime with { RepeatStepSize = 1 }) },
        { "one-time with step unit", Job(schedule: OneTime with { RepeatStepUnit = RepeatStepUnit.Hour }) },
        { "relative endpoint", Job(launch: Launch with { Endpoint = "/hooks/x" }) },
        { "non-http endpoint", Job(launch: Launch with { Endpoint = "ftp://example.com/x" }) },
        { "empty endpoint", Job(launch: Launch with { Endpoint = "" }) },
    };

    [Theory]
    [MemberData(nameof(InvalidJobs))]
    public async Task CreateJob_InvalidInput_ThrowsArgumentException_BeforeOpeningConnection(string _, NewJob job)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => Repository.CreateJobAsync(job, CancellationToken.None));
    }

    [Fact]
    public async Task CreateJob_Null_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Repository.CreateJobAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateJob_MaxLengthNameAndOneTimeSchedule_PassesValidationAndReachesTheConnection()
    {
        // Valid input must get as far as opening a connection, which the fake refuses.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Repository.CreateJobAsync(Job(name: new string('x', 200), schedule: OneTime), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateJob_InvalidSchedule_ThrowsArgumentException_BeforeOpeningConnection()
    {
        var cmd = new UpdateJob
        {
            JobId = Guid.NewGuid(),
            ExpectedVersion = 1,
            Name = "job",
            ConcurrencyMode = ConcurrencyMode.Singleton,
            Schedule = Daily with { RepeatStepSize = 0 },
            LaunchDetails = Launch,
        };

        await Assert.ThrowsAnyAsync<ArgumentException>(() => Repository.UpdateJobAsync(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task GetPendingOutbox_NonPositiveBatchSize_Throws_BeforeOpeningConnection()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Repository.GetPendingOutboxAsync(0, CancellationToken.None));
    }
}
