using System.Text.Json;
using JobScheduler.Dal.Common;
using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobOrchestrator;

namespace JobScheduler.Dal.IntegrationTests;

[Collection("orchestrator")]
public class ClaimTests(AzureSqlFixture fixture, ITestOutputHelper output) : IntegrationTestBase(fixture, output)
{
    private const int BatchSize = 100;
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    private const string RunOutboxSql = """
        SELECT Id, RunId, EventType, Payload, ScheduledEnqueueAt, CreatedAt, ProcessedAt, Attempts
        FROM dbo.JobRunOutbox WHERE RunId = @RunId;
        """;

    private const string RunCountSql = "SELECT COUNT(*) FROM dbo.JobRun WHERE JobId = @JobId;";

    private const string InsertRunSql = """
        INSERT INTO dbo.JobRun (Id, JobId, ScheduledFor, Status, Deadline, CreatedAt)
        VALUES (@Id, @JobId, @ScheduledFor, N'Pending', @Deadline, @CreatedAt);
        SELECT @@ROWCOUNT;
        """;

    [Fact]
    public async Task Claim_ParallelClaimers_ClaimOnce()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var snapshot = await SeedAsync(Builders.DueSnapshot(now));

        var results = await Task.WhenAll(
            Fixture.Runs.ClaimDueJobsAsync(now, BatchSize, Lease, Ct),
            Fixture.Runs.ClaimDueJobsAsync(now, BatchSize, Lease, Ct));

        Assert.Single(results.SelectMany(r => r), c => c.JobId == snapshot.JobId);
        Assert.Equal(1, await PrimaryDb.ScalarAsync<JobOrchestratorDb, int>(
            Fixture.JobOrchestratorConnections, RunCountSql, new { snapshot.JobId }, Ct));
    }

    [Fact]
    public async Task Claim_AdvancesNextRunAt_Recurring()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var snapshot = await SeedAsync(Builders.DueSnapshot(now));

        var claimed = Assert.Single(await ClaimAsync(now, snapshot.JobId));

        var expectedNext = snapshot.NextRunAt.AddMinutes(1);
        Assert.Equal(JobRunStatus.Pending, claimed.Status);
        Assert.Equal(snapshot.NextRunAt, claimed.ScheduledFor);
        Assert.Equal(expectedNext, claimed.NextRunAt);

        var stored = await Fixture.Snapshots.GetAsync(snapshot.JobId, ReadConsistency.Strong, Ct);
        Assert.NotNull(stored);
        Assert.Equal(JobStatus.Active, stored.Status);
        Assert.Equal(expectedNext, stored.NextRunAt);

        var run = await Fixture.Runs.GetRunAsync(claimed.RunId, ReadConsistency.Strong, Ct);
        Assert.NotNull(run);
        Assert.Equal(JobRunStatus.Pending, run.Status);
        Assert.Equal(now + Lease, run.Deadline);
    }

    [Fact]
    public async Task Claim_OneTime_DeactivatesSnapshot()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var snapshot = await SeedAsync(Builders.DueSnapshot(now, scheduleType: ScheduleType.OneTime));

        var claimed = Assert.Single(await ClaimAsync(now, snapshot.JobId));

        Assert.Equal(JobRunStatus.Pending, claimed.Status);
        Assert.Null(claimed.NextRunAt);
        var stored = await Fixture.Snapshots.GetAsync(snapshot.JobId, ReadConsistency.Strong, Ct);
        Assert.Equal(JobStatus.Inactive, stored?.Status);

        // A second claim at a later time finds nothing: the one-time job is already spent.
        Assert.Empty(await ClaimAsync(now.AddMinutes(10), snapshot.JobId));
    }

    [Fact]
    public async Task Claim_EditedFiredOneTime_NotClaimedAgain()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var snapshot = await SeedAsync(Builders.DueSnapshot(now, scheduleType: ScheduleType.OneTime));
        Assert.Single(await ClaimAsync(now, snapshot.JobId));

        // An edit with the same StartAt arrives as an Active snapshot, but the job already fired at that time.
        var edited = snapshot with { Version = 2, Status = JobStatus.Active };
        Assert.Equal(SnapshotUpsertResult.Updated, await Fixture.Snapshots.UpsertAsync(edited, Ct));

        var stored = await Fixture.Snapshots.GetAsync(snapshot.JobId, ReadConsistency.Strong, Ct);
        Assert.Equal(JobStatus.Inactive, stored?.Status);
        Assert.Empty(await ClaimAsync(now.AddMinutes(10), snapshot.JobId));
        Assert.Equal(1, await PrimaryDb.ScalarAsync<JobOrchestratorDb, int>(
            Fixture.JobOrchestratorConnections, RunCountSql, new { snapshot.JobId }, Ct));

        // Moving StartAt to a new time reactivates the job.
        var moved = snapshot with { Version = 3, Status = JobStatus.Active, NextRunAt = snapshot.NextRunAt.AddHours(1) };
        Assert.Equal(SnapshotUpsertResult.Updated, await Fixture.Snapshots.UpsertAsync(moved, Ct));
        stored = await Fixture.Snapshots.GetAsync(snapshot.JobId, ReadConsistency.Strong, Ct);
        Assert.Equal(JobStatus.Active, stored?.Status);
    }

    [Fact]
    public async Task Claim_RunAlreadyExists_SkipsRowAndClaimsOthers()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var existing = await SeedAsync(Builders.DueSnapshot(now));
        var other = await SeedAsync(Builders.DueSnapshot(now));

        var existingRunId = Ids.NewId();
        Assert.Equal(1, await PrimaryDb.ScalarAsync<JobOrchestratorDb, int>(
            Fixture.JobOrchestratorConnections, InsertRunSql,
            new { Id = existingRunId, existing.JobId, ScheduledFor = existing.NextRunAt, Deadline = now + Lease, CreatedAt = now }, Ct));

        var claimed = await Fixture.Runs.ClaimDueJobsAsync(now, BatchSize, Lease, Ct);

        Assert.Single(claimed, c => c.JobId == other.JobId);
        Assert.DoesNotContain(claimed, c => c.JobId == existing.JobId);
        Assert.Equal(1, await PrimaryDb.ScalarAsync<JobOrchestratorDb, int>(
            Fixture.JobOrchestratorConnections, RunCountSql, new { existing.JobId }, Ct));
        Assert.Empty(await PrimaryDb.QueryAsync<JobOrchestratorDb, RunOutboxMessage>(
            Fixture.JobOrchestratorConnections, RunOutboxSql, new { RunId = existingRunId }, Ct));
    }

    [Fact]
    public async Task Claim_Singleton_RunAlreadyExists_LeavesLockUntouched()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var snapshot = await SeedAsync(Builders.DueSnapshot(now, ConcurrencyMode.Singleton));

        Assert.Equal(1, await PrimaryDb.ScalarAsync<JobOrchestratorDb, int>(
            Fixture.JobOrchestratorConnections, InsertRunSql,
            new { Id = Ids.NewId(), snapshot.JobId, ScheduledFor = snapshot.NextRunAt, Deadline = now + Lease, CreatedAt = now }, Ct));

        Assert.Empty(await ClaimAsync(now, snapshot.JobId));

        var stored = await Fixture.Snapshots.GetAsync(snapshot.JobId, ReadConsistency.Strong, Ct);
        Assert.NotNull(stored);
        Assert.Null(stored.CurrentRunId);
        Assert.Null(stored.LockedUntil);
    }

    [Fact]
    public async Task Claim_Singleton_SecondWindowLockFailed()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var snapshot = await SeedAsync(Builders.DueSnapshot(now, ConcurrencyMode.Singleton));

        var first = Assert.Single(await ClaimAsync(now, snapshot.JobId));
        var second = Assert.Single(await ClaimAsync(now.AddMinutes(1), snapshot.JobId));

        Assert.Equal(JobRunStatus.Pending, first.Status);
        Assert.Equal(JobRunStatus.LockFailed, second.Status);

        var locked = await Fixture.Snapshots.GetAsync(snapshot.JobId, ReadConsistency.Strong, Ct);
        Assert.Equal(first.RunId, locked?.CurrentRunId);

        var run = await Fixture.Runs.GetRunAsync(second.RunId, ReadConsistency.Strong, Ct);
        Assert.NotNull(run);
        Assert.Equal(JobRunStatus.LockFailed, run.Status);
        Assert.NotNull(run.FinishedAt);
        Assert.Contains(first.RunId.ToString(), run.ErrorSummary, StringComparison.OrdinalIgnoreCase);

        Assert.Empty(await PrimaryDb.QueryAsync<JobOrchestratorDb, RunOutboxMessage>(
            Fixture.JobOrchestratorConnections, RunOutboxSql, new { second.RunId }, Ct));
    }

    [Fact]
    public async Task Claim_CreatesExecuteJobOutbox()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var snapshot = await SeedAsync(Builders.DueSnapshot(now));

        var claimed = Assert.Single(await ClaimAsync(now, snapshot.JobId));

        var message = Assert.Single(await PrimaryDb.QueryAsync<JobOrchestratorDb, RunOutboxMessage>(
            Fixture.JobOrchestratorConnections, RunOutboxSql, new { claimed.RunId }, Ct));
        Assert.Equal(RunOutboxEventType.ExecuteJob, message.EventType);
        Assert.Equal(claimed.ScheduledFor, message.ScheduledEnqueueAt);
        Assert.Null(message.ProcessedAt);

        var payload = JsonSerializer.Deserialize<ExecuteJobPayload>(message.Payload, Json.Default);
        Assert.NotNull(payload);
        Assert.Equal(claimed.RunId, payload.RunId);
        Assert.Equal(snapshot.JobId, payload.JobId);
        Assert.Equal(claimed.ScheduledFor, payload.ScheduledFor);
        Assert.Equal(now + Lease, payload.Deadline);
        Assert.Equal(Builders.LaunchDetails().Endpoint, payload.LaunchDetails.Endpoint);
    }

    private async Task<JobScheduleSnapshot> SeedAsync(JobScheduleSnapshot snapshot)
    {
        Cleaner.TrackSnapshot(snapshot.JobId);
        await Fixture.Snapshots.UpsertAsync(snapshot, Ct);
        return snapshot;
    }

    // Other due rows in the same synthetic window may be claimed too; tests look only at their own job.
    private async Task<IReadOnlyList<ClaimedRun>> ClaimAsync(DateTime now, Guid jobId) =>
        [.. (await Fixture.Runs.ClaimDueJobsAsync(now, BatchSize, Lease, Ct)).Where(c => c.JobId == jobId)];
}
