using JobScheduler.Dal.Connections;

namespace JobScheduler.Dal.IntegrationTests;

public class ReplicationLagTests(AzureSqlFixture fixture, ITestOutputHelper output) : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task CreateJob_StrongRead_ImmediatelyVisible()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await Fixture.Jobs.CreateJobAsync(Builders.NewJob(), Ct);
        Cleaner.TrackJob(created.Id);

        var read = await Fixture.Jobs.GetJobAsync(created.Id, ReadConsistency.Strong, Ct);

        Assert.NotNull(read);
        Assert.Equal(created.Name, read.Name);
    }

    [Fact]
    public async Task CreateJob_EventualRead_BecomesVisible()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var created = await Fixture.Jobs.CreateJobAsync(Builders.NewJob(), Ct);
        Cleaner.TrackJob(created.Id);

        var (read, elapsed) = await Polling.UntilAsync(
            () => Fixture.Jobs.GetJobAsync(created.Id, ReadConsistency.Eventual, Ct),
            job => job is not null,
            Ct);

        Output.WriteLine($"Readable-secondary lag observed (job-details, Business Critical): {elapsed.TotalMilliseconds:F0} ms");
        Assert.True(read is not null, $"Job not visible on the readable secondary within {Polling.ReplicaTimeout.TotalSeconds:F0} s");
        Assert.Equal(created.Id, read.Id);
    }

    [Fact]
    public async Task SnapshotUpsert_EventualRead_BecomesVisible()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var snapshot = Builders.IdleSnapshot();
        Cleaner.TrackSnapshot(snapshot.JobId);
        await Fixture.Snapshots.UpsertAsync(snapshot, Ct);

        var (read, elapsed) = await Polling.UntilAsync(
            () => Fixture.Snapshots.GetAsync(snapshot.JobId, ReadConsistency.Eventual, Ct),
            s => s is not null,
            Ct);

        Output.WriteLine($"HA-replica lag observed (job-orchestrator): {elapsed.TotalMilliseconds:F0} ms");
        Assert.True(read is not null, $"Snapshot not visible on the HA replica within {Polling.ReplicaTimeout.TotalSeconds:F0} s");
        Assert.Equal(snapshot.JobId, read.JobId);
    }
}
