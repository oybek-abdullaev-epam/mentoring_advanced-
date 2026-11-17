using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobDetails;
using JobScheduler.Dal.JobOrchestrator;

namespace JobScheduler.Dal.IntegrationTests;

[Collection("orchestrator")]
public class RetryTests(AzureSqlFixture fixture, ITestOutputHelper output) : IntegrationTestBase(fixture, output)
{
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    private const string JobDetailsCountsSql = """
        SELECT (SELECT COUNT(*) FROM dbo.Job WHERE Id = @JobId), (SELECT COUNT(*) FROM dbo.JobOutbox WHERE JobId = @JobId);
        """;

    private const string RunCountsSql = """
        SELECT (SELECT COUNT(*) FROM dbo.JobRun WHERE JobId = @JobId),
               (SELECT COUNT(*) FROM dbo.JobRunOutbox o JOIN dbo.JobRun r ON r.Id = o.RunId WHERE r.JobId = @JobId);
        """;

    private JobRepository RetryingJobs() =>
        new(new CommitThenFailConnectionFactory<JobDetailsDb>(Fixture.JobDetailsConnections), TimeProvider.System);

    private JobRunRepository RetryingRuns() =>
        new(new CommitThenFailConnectionFactory<JobOrchestratorDb>(Fixture.JobOrchestratorConnections), TimeProvider.System);

    [Fact]
    public async Task Retry_CreateJob_AfterCommittedAttempt_ReturnsSameJob()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var job = Builders.NewJob();
        var created = await RetryingJobs().CreateJobAsync(job, Ct);
        Cleaner.TrackJob(created.Id);

        Assert.Equal(1, created.Version);
        Assert.Equal(job.Name, created.Name);
        var counts = (await PrimaryDb.QueryAsync<JobDetailsDb, (int Job, int Outbox)>(
            Fixture.JobDetailsConnections, JobDetailsCountsSql, new { JobId = created.Id }, Ct)).Single();
        Assert.Equal((1, 1), counts);
    }

    [Fact]
    public async Task Retry_UpdateJob_AfterCommittedAttempt_ReturnsNewVersion()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await Fixture.Jobs.CreateJobAsync(Builders.NewJob(), Ct);
        Cleaner.TrackJob(created.Id);

        var newVersion = await RetryingJobs().UpdateJobAsync(Builders.UpdateOf(created, created.Version, Builders.UniqueName()), Ct);

        Assert.Equal(2, newVersion);
        var stored = await Fixture.Jobs.GetJobAsync(created.Id, ReadConsistency.Strong, Ct);
        Assert.Equal(2, stored?.Version);
    }

    [Fact]
    public async Task Retry_SoftDeleteJob_AfterCommittedAttempt_ReturnsNewVersion()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await Fixture.Jobs.CreateJobAsync(Builders.NewJob(), Ct);
        Cleaner.TrackJob(created.Id);

        var newVersion = await RetryingJobs().SoftDeleteJobAsync(created.Id, created.Version, Ct);

        Assert.Equal(2, newVersion);
        var stored = await Fixture.Jobs.GetJobAsync(created.Id, ReadConsistency.Strong, Ct);
        Assert.Equal(JobStatus.Deleted, stored?.Status);
        var counts = (await PrimaryDb.QueryAsync<JobDetailsDb, (int Job, int Outbox)>(
            Fixture.JobDetailsConnections, JobDetailsCountsSql, new { JobId = created.Id }, Ct)).Single();
        Assert.Equal((1, 2), counts);
    }

    [Fact]
    public async Task Retry_TryUpdateStatus_AfterCommittedAttempt_ReturnsTrue()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var run = await ClaimAsync(now, ConcurrencyMode.Concurrent);

        Assert.True(await RetryingRuns().TryUpdateStatusAsync(run.RunId, JobRunStatus.Pending, JobRunStatus.Running, now.AddSeconds(1), null, Ct));

        var stored = await Fixture.Runs.GetRunAsync(run.RunId, ReadConsistency.Strong, Ct);
        Assert.Equal(JobRunStatus.Running, stored?.Status);
    }

    [Fact]
    public async Task Retry_ReleaseLock_AfterCommittedAttempt_LockReleased()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var run = await ClaimAsync(Builders.SyntheticNow(), ConcurrencyMode.Singleton);

        await RetryingRuns().ReleaseLockAsync(run.JobId, run.RunId, Ct);

        var snapshot = await Fixture.Snapshots.GetAsync(run.JobId, ReadConsistency.Strong, Ct);
        Assert.Null(snapshot?.CurrentRunId);
    }

    [Fact]
    public async Task Retry_Claim_AfterCommittedAttempt_NoDuplicateRun()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var snapshot = Builders.DueSnapshot(now);
        Cleaner.TrackSnapshot(snapshot.JobId);
        await Fixture.Snapshots.UpsertAsync(snapshot, Ct);

        // The retried claim returns the next due batch, so the list may not contain this job: the rows are the record.
        await RetryingRuns().ClaimDueJobsAsync(now, 100, Lease, Ct);

        var counts = (await PrimaryDb.QueryAsync<JobOrchestratorDb, (int Runs, int Outbox)>(
            Fixture.JobOrchestratorConnections, RunCountsSql, new { snapshot.JobId }, Ct)).Single();
        Assert.Equal((1, 1), counts);
    }

    private async Task<ClaimedRun> ClaimAsync(DateTime now, ConcurrencyMode mode)
    {
        var snapshot = Builders.DueSnapshot(now, mode);
        Cleaner.TrackSnapshot(snapshot.JobId);
        await Fixture.Snapshots.UpsertAsync(snapshot, Ct);

        var claimed = await Fixture.Runs.ClaimDueJobsAsync(now, 100, Lease, Ct);
        return Assert.Single(claimed, c => c.JobId == snapshot.JobId);
    }
}
