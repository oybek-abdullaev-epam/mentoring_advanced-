using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobOrchestrator;

namespace JobScheduler.Dal.IntegrationTests;

[Collection("orchestrator")]
public class RunStatusTests(AzureSqlFixture fixture, ITestOutputHelper output) : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task StatusCas_DoesNotOverwriteTerminal()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var run = await ClaimAsync(now, ConcurrencyMode.Concurrent);

        Assert.True(await Fixture.Runs.TryUpdateStatusAsync(run.RunId, JobRunStatus.Pending, JobRunStatus.Running, now.AddSeconds(1), null, Ct));

        // Repeating a call whose target is already reached succeeds without rewriting StartedAt.
        Assert.True(await Fixture.Runs.TryUpdateStatusAsync(run.RunId, JobRunStatus.Pending, JobRunStatus.Running, now.AddSeconds(2), null, Ct));
        Assert.True(await Fixture.Runs.TryUpdateStatusAsync(run.RunId, JobRunStatus.Running, JobRunStatus.Succeeded, now.AddSeconds(3), null, Ct));
        Assert.True(await Fixture.Runs.TryUpdateStatusAsync(run.RunId, JobRunStatus.Running, JobRunStatus.Succeeded, now.AddSeconds(4), null, Ct));

        // A late timeout or failure report must not overwrite the terminal status.
        Assert.False(await Fixture.Runs.TryUpdateStatusAsync(run.RunId, JobRunStatus.Running, JobRunStatus.TimedOut, now.AddSeconds(4), "late", Ct));
        Assert.False(await Fixture.Runs.TryUpdateStatusAsync(run.RunId, JobRunStatus.Running, JobRunStatus.Failed, now.AddSeconds(5), "late", Ct));

        var stored = await Fixture.Runs.GetRunAsync(run.RunId, ReadConsistency.Strong, Ct);
        Assert.NotNull(stored);
        Assert.Equal(JobRunStatus.Succeeded, stored.Status);
        Assert.Equal(now.AddSeconds(1), stored.StartedAt);
        Assert.Equal(now.AddSeconds(3), stored.FinishedAt);
        Assert.Null(stored.ErrorSummary);
    }

    [Fact]
    public async Task ReleaseLock_OnlyOwningRun()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var run = await ClaimAsync(now, ConcurrencyMode.Singleton);

        // Not the owner: a no-op, the owner's lock stays.
        await Fixture.Runs.ReleaseLockAsync(run.JobId, Guid.NewGuid(), Ct);
        var stillLocked = await Fixture.Snapshots.GetAsync(run.JobId, ReadConsistency.Strong, Ct);
        Assert.NotNull(stillLocked);
        Assert.Equal(run.RunId, stillLocked.CurrentRunId);
        Assert.NotNull(stillLocked.LockedUntil);

        await Fixture.Runs.ReleaseLockAsync(run.JobId, run.RunId, Ct);
        var released = await Fixture.Snapshots.GetAsync(run.JobId, ReadConsistency.Strong, Ct);
        Assert.NotNull(released);
        Assert.Null(released.CurrentRunId);
        Assert.Null(released.LockedUntil);

        await Fixture.Runs.ReleaseLockAsync(run.JobId, run.RunId, Ct);
    }

    private async Task<ClaimedRun> ClaimAsync(DateTime now, ConcurrencyMode mode)
    {
        var snapshot = Builders.DueSnapshot(now, mode);
        Cleaner.TrackSnapshot(snapshot.JobId);
        await Fixture.Snapshots.UpsertAsync(snapshot, Ct);

        var claimed = await Fixture.Runs.ClaimDueJobsAsync(now, 100, TimeSpan.FromMinutes(5), Ct);
        var run = Assert.Single(claimed, c => c.JobId == snapshot.JobId);
        Assert.Equal(JobRunStatus.Pending, run.Status);
        return run;
    }
}
