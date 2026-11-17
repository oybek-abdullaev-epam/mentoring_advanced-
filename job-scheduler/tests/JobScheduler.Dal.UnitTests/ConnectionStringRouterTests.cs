using JobScheduler.Dal.Connections;
using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.UnitTests;

public class ConnectionStringRouterTests
{
    private const string OrchestratorPrimary = "Server=tcp:sql-x-eus.database.windows.net,1433;Database=job-orchestrator;Encrypt=True;";
    private const string DetailsPrimary = "Server=tcp:sql-x-eus.database.windows.net,1433;Database=job-details;Encrypt=True;";

    private const string PrimaryHost = "tcp:sql-x-eus.database.windows.net,1433";

    private static DatabaseOptions Orchestrator(string? reporting = "job-orchestrator-reporting") =>
        new() { ConnectionString = OrchestratorPrimary, ReportingDatabase = reporting };

    private static DatabaseOptions Details() => new() { ConnectionString = DetailsPrimary };

    private static SqlConnectionStringBuilder Parse(ConnectionRoute route) => new(route.ConnectionString);

    [Fact]
    public void ReadWrite_KeepsServerCatalogAndIntent()
    {
        var route = ConnectionStringRouter.Resolve(Orchestrator(), DbRole.ReadWrite);
        var b = Parse(route);

        Assert.Equal(PrimaryHost, b.DataSource);
        Assert.Equal("job-orchestrator", b.InitialCatalog);
        Assert.Equal(ApplicationIntent.ReadWrite, b.ApplicationIntent);
        Assert.Equal(DbRole.ReadWrite, route.RequestedRole);
        Assert.Equal(DbRole.ReadWrite, route.EffectiveRole);
        Assert.Equal(PrimaryHost, route.DataSource);
        Assert.Equal("job-orchestrator", route.Database);
        Assert.Equal(ApplicationIntent.ReadWrite, route.Intent);
    }

    [Fact]
    public void ReadOnly_KeepsServerAndCatalogAndSetsReadOnlyIntent()
    {
        var route = ConnectionStringRouter.Resolve(Orchestrator(), DbRole.ReadOnly);
        var b = Parse(route);

        Assert.Equal(PrimaryHost, b.DataSource);
        Assert.Equal("job-orchestrator", b.InitialCatalog);
        Assert.Equal(ApplicationIntent.ReadOnly, b.ApplicationIntent);
        Assert.Equal(DbRole.ReadOnly, route.RequestedRole);
        Assert.Equal(DbRole.ReadOnly, route.EffectiveRole);
        Assert.Equal(PrimaryHost, route.DataSource);
    }

    [Fact]
    public void ReadOnly_JobDetails_StaysOnPrimaryServerWithReadOnlyIntent()
    {
        var route = ConnectionStringRouter.Resolve(Details(), DbRole.ReadOnly);
        var b = Parse(route);

        Assert.Equal(PrimaryHost, b.DataSource);
        Assert.Equal("job-details", b.InitialCatalog);
        Assert.Equal(ApplicationIntent.ReadOnly, b.ApplicationIntent);
        Assert.Equal(DbRole.ReadOnly, route.EffectiveRole);
    }

    [Fact]
    public void Reporting_WithReportingDatabase_SwapsCatalogOnPrimaryServer()
    {
        var route = ConnectionStringRouter.Resolve(Orchestrator(), DbRole.Reporting);
        var b = Parse(route);

        Assert.Equal(PrimaryHost, b.DataSource);
        Assert.Equal("job-orchestrator-reporting", b.InitialCatalog);
        Assert.Equal(ApplicationIntent.ReadOnly, b.ApplicationIntent);
        Assert.Equal(DbRole.Reporting, route.RequestedRole);
        Assert.Equal(DbRole.Reporting, route.EffectiveRole);
        Assert.Equal("job-orchestrator-reporting", route.Database);
    }

    [Fact]
    public void Reporting_WithoutReportingDatabase_FallsBackToReadOnly()
    {
        var route = ConnectionStringRouter.Resolve(Orchestrator(reporting: null), DbRole.Reporting);
        var b = Parse(route);

        Assert.Equal(DbRole.Reporting, route.RequestedRole);
        Assert.Equal(DbRole.ReadOnly, route.EffectiveRole);
        Assert.Equal(PrimaryHost, b.DataSource);
        Assert.Equal("job-orchestrator", b.InitialCatalog);
        Assert.Equal(ApplicationIntent.ReadOnly, b.ApplicationIntent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyConnectionString_Throws(string connectionString)
    {
        var options = new DatabaseOptions { ConnectionString = connectionString };

        foreach (var role in Enum.GetValues<DbRole>())
        {
            Assert.Throws<InvalidOperationException>(() => ConnectionStringRouter.Resolve(options, role));
        }
    }

    [Fact]
    public void Resolve_DoesNotMutateOptions()
    {
        var options = new DatabaseOptions
        {
            ConnectionString = OrchestratorPrimary,
            ReportingDatabase = "job-orchestrator-reporting",
        };

        foreach (var role in Enum.GetValues<DbRole>())
        {
            ConnectionStringRouter.Resolve(options, role);
        }

        Assert.Equal(OrchestratorPrimary, options.ConnectionString);
        Assert.Equal("job-orchestrator-reporting", options.ReportingDatabase);
    }

    [Fact]
    public void Routes_NeverContainAuthenticationOrCredentials()
    {
        foreach (var role in Enum.GetValues<DbRole>())
        {
            var b = Parse(ConnectionStringRouter.Resolve(Details(), role));
            Assert.Equal(SqlAuthenticationMethod.NotSpecified, b.Authentication);
            Assert.Empty(b.UserID);
        }
    }

    [Theory]
    [InlineData(ReadConsistency.Strong, DbRole.ReadWrite)]
    [InlineData(ReadConsistency.Eventual, DbRole.ReadOnly)]
    public void ReadConsistency_MapsToRole(ReadConsistency consistency, DbRole expected) =>
        Assert.Equal(expected, consistency.ToRole());
}
