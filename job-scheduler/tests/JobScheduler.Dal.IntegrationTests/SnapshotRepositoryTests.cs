using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobOrchestrator;

namespace JobScheduler.Dal.IntegrationTests;

[Collection("orchestrator")]
public class SnapshotRepositoryTests(AzureSqlFixture fixture, ITestOutputHelper output) : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task SnapshotUpsert_InsertThenUpdate_IgnoresStale()
    {
        Assert.SkipWhen(Fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var v1 = Builders.IdleSnapshot();
        Cleaner.TrackSnapshot(v1.JobId);
        var v2 = v1 with { Version = 2, Status = JobStatus.Inactive, RepeatStepSize = 5, NextRunAt = v1.NextRunAt.AddHours(1) };

        Assert.Equal(SnapshotUpsertResult.Inserted, await Fixture.Snapshots.UpsertAsync(v1, Ct));
        Assert.Equal(SnapshotUpsertResult.Updated, await Fixture.Snapshots.UpsertAsync(v2, Ct));
        Assert.Equal(SnapshotUpsertResult.IgnoredStale, await Fixture.Snapshots.UpsertAsync(v1, Ct));
        Assert.Equal(SnapshotUpsertResult.IgnoredStale, await Fixture.Snapshots.UpsertAsync(v2, Ct));

        var stored = await Fixture.Snapshots.GetAsync(v1.JobId, ReadConsistency.Strong, Ct);
        Assert.NotNull(stored);
        Assert.Equal(2, stored.Version);
        Assert.Equal(JobStatus.Inactive, stored.Status);
        Assert.Equal(5, stored.RepeatStepSize);
        Assert.Equal(v2.NextRunAt, stored.NextRunAt);
    }
}
