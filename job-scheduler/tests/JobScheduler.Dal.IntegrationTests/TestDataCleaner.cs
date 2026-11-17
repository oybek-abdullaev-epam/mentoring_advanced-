using Dapper;
using JobScheduler.Dal.Connections;

namespace JobScheduler.Dal.IntegrationTests;

// Deletes only the rows of the JobIds a test registered, children first, always on the primaries.
public sealed class TestDataCleaner(AzureSqlFixture fixture)
{
    private const string DeleteJobDetailsSql = """
        DELETE FROM dbo.JobOutbox WHERE JobId IN @JobIds;
        DELETE FROM dbo.JobLaunchDetails WHERE JobId IN @JobIds;
        DELETE FROM dbo.JobSchedule WHERE JobId IN @JobIds;
        DELETE FROM dbo.Job WHERE Id IN @JobIds;
        """;

    private const string DeleteOrchestratorSql = """
        DELETE o FROM dbo.JobRunOutbox o JOIN dbo.JobRun r ON r.Id = o.RunId WHERE r.JobId IN @JobIds;
        DELETE FROM dbo.JobRun WHERE JobId IN @JobIds;
        DELETE FROM dbo.JobScheduleSnapshot WHERE JobId IN @JobIds;
        """;

    private readonly List<Guid> _jobDetailsIds = [];
    private readonly List<Guid> _orchestratorIds = [];

    public Guid TrackJob(Guid jobId)
    {
        lock (_jobDetailsIds)
        {
            _jobDetailsIds.Add(jobId);
        }

        return jobId;
    }

    public Guid TrackSnapshot(Guid jobId)
    {
        lock (_orchestratorIds)
        {
            _orchestratorIds.Add(jobId);
        }

        return jobId;
    }

    public async ValueTask CleanAsync()
    {
        if (fixture.NotConfigured)
        {
            return;
        }

        Guid[] jobDetailsIds;
        lock (_jobDetailsIds)
        {
            jobDetailsIds = [.. _jobDetailsIds];
        }

        Guid[] orchestratorIds;
        lock (_orchestratorIds)
        {
            orchestratorIds = [.. _orchestratorIds];
        }

        if (jobDetailsIds.Length > 0)
        {
            await DeleteAsync(fixture.JobDetailsConnections, DeleteJobDetailsSql, jobDetailsIds);
        }

        if (orchestratorIds.Length > 0)
        {
            await DeleteAsync(fixture.JobOrchestratorConnections, DeleteOrchestratorSql, orchestratorIds);
        }
    }

    private static async Task DeleteAsync<T>(ISqlConnectionFactory<T> factory, string sql, Guid[] jobIds) where T : IDatabaseMarker
    {
        await using var connection = await factory.OpenAsync(DbRole.ReadWrite, CancellationToken.None);
        await using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { JobIds = jobIds }, transaction, cancellationToken: CancellationToken.None));
        await transaction.CommitAsync(CancellationToken.None);
    }
}
