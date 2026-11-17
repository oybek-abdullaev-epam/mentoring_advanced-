using System.Text.Json;
using JobScheduler.Dal.Common;
using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobDetails;

namespace JobScheduler.Dal.IntegrationTests;

public class JobRepositoryTests(AzureSqlFixture fixture, ITestOutputHelper output) : IntegrationTestBase(fixture, output)
{
    private const string OutboxSql = """
        SELECT Id, JobId, EventType, JobVersion, Payload, CreatedAt, ProcessedAt, Attempts
        FROM dbo.JobOutbox WHERE JobId = @JobId ORDER BY JobVersion;
        """;

    private const string RowCountsSql = """
        SELECT (SELECT COUNT(*) FROM dbo.Job WHERE Id = @JobId),
               (SELECT COUNT(*) FROM dbo.JobSchedule WHERE JobId = @JobId),
               (SELECT COUNT(*) FROM dbo.JobLaunchDetails WHERE JobId = @JobId),
               (SELECT COUNT(*) FROM dbo.JobOutbox WHERE JobId = @JobId);
        """;

    [Fact]
    public async Task CreateJob_WritesAllFourRows()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await CreateAsync(Builders.NewJob());

        var counts = (await PrimaryDb.QueryAsync<JobDetailsDb, (int Job, int Schedule, int Launch, int Outbox)>(
            Fixture.JobDetailsConnections, RowCountsSql, new { JobId = created.Id }, Ct)).Single();
        Assert.Equal((1, 1, 1, 1), counts);

        Assert.Equal(1, created.Version);
        Assert.Equal(JobStatus.Active, created.Status);

        var outbox = Assert.Single(await PrimaryDb.QueryAsync<JobDetailsDb, JobOutboxMessage>(
            Fixture.JobDetailsConnections, OutboxSql, new { JobId = created.Id }, Ct));
        Assert.Equal(OutboxEventType.JobCreated, outbox.EventType);
        Assert.Equal(1, outbox.JobVersion);
        Assert.Null(outbox.ProcessedAt);

        var payload = JsonSerializer.Deserialize<JobSnapshotPayload>(outbox.Payload, Json.Default);
        Assert.NotNull(payload);
        Assert.Equal(created.Id, payload.JobId);
        Assert.Equal(created.UserId, payload.UserId);
        Assert.Equal(created.Endpoint, payload.LaunchDetails.Endpoint);
    }

    [Fact]
    public async Task CreateJob_DuplicateName_Throws()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var first = await CreateAsync(Builders.NewJob());

        var exception = await Assert.ThrowsAsync<DuplicateJobNameException>(
            () => Fixture.Jobs.CreateJobAsync(Builders.NewJob(first.UserId, first.Name), Ct));

        Assert.Equal(first.UserId, exception.UserId);
        Assert.Equal(first.Name, exception.Name);
    }

    [Fact]
    public async Task UpdateJob_BumpsVersion_AndWritesOutbox()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await CreateAsync(Builders.NewJob());
        var newName = Builders.UniqueName();

        var newVersion = await Fixture.Jobs.UpdateJobAsync(Builders.UpdateOf(created, created.Version, newName), Ct);

        Assert.Equal(2, newVersion);
        var read = await Fixture.Jobs.GetJobAsync(created.Id, ReadConsistency.Strong, Ct);
        Assert.NotNull(read);
        Assert.Equal(newName, read.Name);
        Assert.Equal(2, read.Version);

        var outbox = await PrimaryDb.QueryAsync<JobDetailsDb, JobOutboxMessage>(
            Fixture.JobDetailsConnections, OutboxSql, new { JobId = created.Id }, Ct);
        Assert.Equal([OutboxEventType.JobCreated, OutboxEventType.JobUpdated], outbox.Select(o => o.EventType));
        Assert.Equal([1, 2], outbox.Select(o => o.JobVersion));
    }

    [Fact]
    public async Task UpdateJob_StaleVersion_Throws()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await CreateAsync(Builders.NewJob());
        await Fixture.Jobs.UpdateJobAsync(Builders.UpdateOf(created, created.Version, Builders.UniqueName()), Ct);

        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => Fixture.Jobs.UpdateJobAsync(Builders.UpdateOf(created, created.Version, Builders.UniqueName()), Ct));

        Assert.Equal(created.Id, exception.EntityId);
        Assert.Equal(created.Version, exception.ExpectedVersion);
    }

    [Fact]
    public async Task SoftDelete_StaleVersion_Throws()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await CreateAsync(Builders.NewJob());
        await Fixture.Jobs.UpdateJobAsync(Builders.UpdateOf(created, created.Version, Builders.UniqueName()), Ct);

        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => Fixture.Jobs.SoftDeleteJobAsync(created.Id, created.Version, Ct));

        Assert.Equal(created.Id, exception.EntityId);
        Assert.Equal(created.Version, exception.ExpectedVersion);
        var stored = await Fixture.Jobs.GetJobAsync(created.Id, ReadConsistency.Strong, Ct);
        Assert.Equal(JobStatus.Active, stored?.Status);
    }

    [Fact]
    public async Task SoftDelete_AllowsReuseOfName()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await CreateAsync(Builders.NewJob());

        var newVersion = await Fixture.Jobs.SoftDeleteJobAsync(created.Id, created.Version, Ct);

        Assert.Equal(2, newVersion);
        var deleted = await Fixture.Jobs.GetJobAsync(created.Id, ReadConsistency.Strong, Ct);
        Assert.Equal(JobStatus.Deleted, deleted?.Status);

        var reused = await CreateAsync(Builders.NewJob(created.UserId, created.Name));
        Assert.NotEqual(created.Id, reused.Id);
        Assert.Equal(created.Name, reused.Name);
    }

    [Fact]
    public async Task Outbox_Pending_ThenProcessed()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await CreateAsync(Builders.NewJob());

        var pending = await Fixture.Jobs.GetPendingOutboxAsync(1000, Ct);
        var message = Assert.Single(pending, m => m.JobId == created.Id);
        Assert.Equal(OutboxEventType.JobCreated, message.EventType);

        await Fixture.Jobs.MarkOutboxProcessedAsync(message.Id, Ct);

        var stored = Assert.Single(await PrimaryDb.QueryAsync<JobDetailsDb, JobOutboxMessage>(
            Fixture.JobDetailsConnections, OutboxSql, new { JobId = created.Id }, Ct));
        Assert.NotNull(stored.ProcessedAt);
        Assert.Equal(1, stored.Attempts);
        Assert.DoesNotContain(await Fixture.Jobs.GetPendingOutboxAsync(1000, Ct), m => m.Id == message.Id);
    }

    [Fact]
    public async Task Outbox_FailedAttempt_StaysPending()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await CreateAsync(Builders.NewJob());
        var message = Assert.Single(await Fixture.Jobs.GetPendingOutboxAsync(1000, Ct), m => m.JobId == created.Id);

        await Fixture.Jobs.MarkOutboxFailedAttemptAsync(message.Id, Ct);

        var stored = Assert.Single(await PrimaryDb.QueryAsync<JobDetailsDb, JobOutboxMessage>(
            Fixture.JobDetailsConnections, OutboxSql, new { JobId = created.Id }, Ct));
        Assert.Null(stored.ProcessedAt);
        Assert.Equal(1, stored.Attempts);
    }

    private async Task<JobDetailsView> CreateAsync(NewJob job)
    {
        var created = await Fixture.Jobs.CreateJobAsync(job, Ct);
        Cleaner.TrackJob(created.Id);
        return created;
    }
}
