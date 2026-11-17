using JobScheduler.Dal.Connections;
using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.IntegrationTests;

public class RoutingTests(AzureSqlFixture fixture, ITestOutputHelper output)
{
    private const string ProbeSql =
        "SELECT DB_NAME(), CAST(DATABASEPROPERTYEX(DB_NAME(), 'Updateability') AS NVARCHAR(20)), @@SERVERNAME";

    private const string ReplicaWriteJobDetailsSql = """
        INSERT INTO dbo.Job (Id, UserId, Name, Status, ConcurrencyMode)
        VALUES (@Id, @Id, N'replica-write-probe', N'Active', N'Singleton');
        """;

    private const string ReplicaWriteOrchestratorSql = """
        INSERT INTO dbo.JobScheduleSnapshot (JobId, UserId, ConcurrencyMode, ScheduleType, NextRunAt, LaunchDetails, Status, Version)
        VALUES (@Id, @Id, N'Singleton', N'OneTime', '2099-01-01', N'{}', N'Active', 1);
        """;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Routing_ReadWrite_HitsWritablePrimary_JobDetails()
    {
        Assert.SkipWhen(fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var probe = await ProbeAsync(fixture.JobDetailsConnections, DbRole.ReadWrite);

        Assert.Equal("job-details", probe.Database);
        Assert.Equal("READ_WRITE", probe.Updateability);
    }

    [Fact]
    public async Task Routing_ReadWrite_HitsWritablePrimary_JobOrchestrator()
    {
        Assert.SkipWhen(fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var probe = await ProbeAsync(fixture.JobOrchestratorConnections, DbRole.ReadWrite);

        Assert.Equal("job-orchestrator", probe.Database);
        Assert.Equal("READ_WRITE", probe.Updateability);
    }

    [Fact]
    public async Task Routing_ReadOnly_HitsReadOnlyReplica_JobDetails()
    {
        Assert.SkipWhen(fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var secondary = await ProbeAsync(fixture.JobDetailsConnections, DbRole.ReadOnly);

        Assert.Equal("job-details", secondary.Database);
        Assert.Equal("READ_ONLY", secondary.Updateability);
    }

    [Fact]
    public async Task Routing_ReadOnly_HitsReadOnlyReplica_JobOrchestrator()
    {
        Assert.SkipWhen(fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var replica = await ProbeAsync(fixture.JobOrchestratorConnections, DbRole.ReadOnly);

        Assert.Equal("job-orchestrator", replica.Database);
        Assert.Equal("READ_ONLY", replica.Updateability);
    }

    [Fact]
    public async Task Routing_Reporting_HitsNamedReplica()
    {
        Assert.SkipWhen(fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);
        Assert.SkipUnless(fixture.HasReportingDatabase, "JobSchedulerDal:JobOrchestrator:ReportingDatabase is not configured");

        var named = await ProbeAsync(fixture.JobOrchestratorConnections, DbRole.Reporting);

        Assert.Equal("job-orchestrator-reporting", named.Database);
        Assert.Equal("READ_ONLY", named.Updateability);
    }

    [Fact]
    public async Task Routing_Reporting_FallsBackToReadOnlyReplica_JobDetails()
    {
        Assert.SkipWhen(fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        var probe = await ProbeAsync(fixture.JobDetailsConnections, DbRole.Reporting);

        Assert.Equal("job-details", probe.Database);
        Assert.Equal("READ_ONLY", probe.Updateability);
    }

    [Fact]
    public async Task Replica_RejectsWrites_JobDetails()
    {
        Assert.SkipWhen(fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        await AssertWriteRejectedAsync(fixture.JobDetailsConnections, DbRole.ReadOnly, ReplicaWriteJobDetailsSql);
    }

    [Fact]
    public async Task Replica_RejectsWrites_JobOrchestrator()
    {
        Assert.SkipWhen(fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);

        await AssertWriteRejectedAsync(fixture.JobOrchestratorConnections, DbRole.ReadOnly, ReplicaWriteOrchestratorSql);
    }

    [Fact]
    public async Task Replica_RejectsWrites_NamedReplica()
    {
        Assert.SkipWhen(fixture.NotConfigured, AzureSqlFixture.NotConfiguredMessage);
        Assert.SkipUnless(fixture.HasReportingDatabase, "JobSchedulerDal:JobOrchestrator:ReportingDatabase is not configured");

        await AssertWriteRejectedAsync(fixture.JobOrchestratorConnections, DbRole.Reporting, ReplicaWriteOrchestratorSql);
    }

    private async Task<RoutingProbe> ProbeAsync<TDb>(ISqlConnectionFactory<TDb> factory, DbRole role) where TDb : IDatabaseMarker
    {
        await using var connection = await factory.OpenAsync(role, Ct);
        await using var command = new SqlCommand(ProbeSql, connection);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        Assert.True(await reader.ReadAsync(Ct));

        var probe = new RoutingProbe(reader.GetString(0), reader.GetString(1), reader.GetString(2));
        output.WriteLine($"{TDb.Name} role={role}: database={probe.Database} updateability={probe.Updateability} server={probe.Server}");
        return probe;
    }

    private async Task AssertWriteRejectedAsync<TDb>(ISqlConnectionFactory<TDb> factory, DbRole role, string insertSql) where TDb : IDatabaseMarker
    {
        await using var connection = await factory.OpenAsync(role, Ct);
        await using var command = new SqlCommand(insertSql, connection);
        command.Parameters.AddWithValue("@Id", Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync(Ct));

        output.WriteLine($"{TDb.Name} write via {role} rejected: error {exception.Number}: {exception.Message}");
        Assert.Equal(3906, exception.Number);
    }

    private sealed record RoutingProbe(string Database, string Updateability, string Server);
}
