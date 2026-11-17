using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobOrchestrator;

namespace JobScheduler.Dal.IntegrationTests;

[Collection("orchestrator")]
public class OverdueRunsTests(AzureSqlFixture fixture, ITestOutputHelper output) : IntegrationTestBase(fixture, output)
{
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task OverdueRuns_PastDeadlineAndNotTerminal_EventuallyVisibleOnReplica()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var stuck = await ClaimAsync(now);
        var finished = await ClaimAsync(now);
        Assert.True(await Fixture.Runs.TryUpdateStatusAsync(finished.RunId, JobRunStatus.Pending, JobRunStatus.Running, now.AddSeconds(1), null, Ct));
        Assert.True(await Fixture.Runs.TryUpdateStatusAsync(finished.RunId, JobRunStatus.Running, JobRunStatus.Succeeded, now.AddSeconds(2), null, Ct));

        // The replica applies the log in order, so once the last write (Succeeded) is visible, the claims are too.
        var (_, elapsed) = await Polling.UntilAsync(
            () => Fixture.Runs.GetRunAsync(finished.RunId, ReadConsistency.Eventual, Ct),
            run => run?.Status == JobRunStatus.Succeeded,
            Ct);
        Output.WriteLine($"HA replica lag observed (job-orchestrator): {elapsed.TotalMilliseconds:F0} ms");

        var overdue = await Fixture.Runs.GetOverdueRunsAsync(PastDeadline(now), 1000, Ct);

        var found = Assert.Single(overdue, r => r.Id == stuck.RunId);
        Assert.Equal(JobRunStatus.Pending, found.Status);
        Assert.DoesNotContain(overdue, r => r.Id == finished.RunId);
    }

    [Fact]
    public async Task OverdueRuns_BeforeDeadline_NotReturned()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var now = Builders.SyntheticNow();
        var run = await ClaimAsync(now);

        // Without waiting for the replica the assertion below would pass trivially, so first see the run there.
        await Polling.UntilAsync<IReadOnlyList<JobRunView>>(
            () => Fixture.Runs.GetOverdueRunsAsync(PastDeadline(now), 1000, Ct)!,
            runs => runs is not null && runs.Any(r => r.Id == run.RunId),
            Ct);

        var beforeDeadline = await Fixture.Runs.GetOverdueRunsAsync(now + Lease - TimeSpan.FromSeconds(1), 1000, Ct);
        Assert.DoesNotContain(beforeDeadline, r => r.Id == run.RunId);
    }

    private static DateTime PastDeadline(DateTime claimedAt) => claimedAt + Lease + TimeSpan.FromMinutes(5);

    private async Task<ClaimedRun> ClaimAsync(DateTime now)
    {
        var snapshot = Builders.DueSnapshot(now);
        Cleaner.TrackSnapshot(snapshot.JobId);
        await Fixture.Snapshots.UpsertAsync(snapshot, Ct);

        var claimed = await Fixture.Runs.ClaimDueJobsAsync(now, 100, Lease, Ct);
        var run = Assert.Single(claimed, c => c.JobId == snapshot.JobId);
        Assert.Equal(JobRunStatus.Pending, run.Status);
        return run;
    }
}
