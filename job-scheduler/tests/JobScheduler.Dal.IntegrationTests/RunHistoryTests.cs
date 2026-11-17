using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobOrchestrator;

namespace JobScheduler.Dal.IntegrationTests;

[Collection("orchestrator")]
public class RunHistoryTests(AzureSqlFixture fixture, ITestOutputHelper output) : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task RunHistory_ServedFromNamedReplica_EventuallyVisible()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);
        Assert.SkipUnless(Fixture.HasReportingDatabase, "JobSchedulerDal:JobOrchestrator:ReportingDatabase is not configured");

        var now = Builders.SyntheticNow();
        var snapshot = Builders.DueSnapshot(now);
        Cleaner.TrackSnapshot(snapshot.JobId);
        await Fixture.Snapshots.UpsertAsync(snapshot, Ct);

        // One-minute recurrence: claims one minute apart produce three consecutive windows.
        for (var minute = 0; minute < 3; minute++)
        {
            await Fixture.Runs.ClaimDueJobsAsync(now.AddMinutes(minute), 100, TimeSpan.FromMinutes(5), Ct);
        }

        var (history, elapsed) = await Polling.UntilAsync<IReadOnlyList<JobRunView>>(
            () => Fixture.Runs.GetRunHistoryAsync(snapshot.JobId, 10, Ct)!,
            runs => runs is { Count: 3 },
            Ct);

        Output.WriteLine($"Named-replica lag observed (job-orchestrator-reporting): {elapsed.TotalMilliseconds:F0} ms");
        Assert.NotNull(history);
        Assert.Equal(3, history.Count);
        Assert.All(history, run => Assert.Equal(snapshot.JobId, run.JobId));
        Assert.Equal(history.OrderByDescending(r => r.ScheduledFor).Select(r => r.Id), history.Select(r => r.Id));
    }
}
