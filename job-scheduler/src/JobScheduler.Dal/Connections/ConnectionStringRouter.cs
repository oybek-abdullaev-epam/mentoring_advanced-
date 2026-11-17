using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.Connections;

// Both databases expose their replicas under the primary's server name:
// - ApplicationIntent=ReadOnly makes the gateway route to a readable replica: the Business Critical readable
//   secondary for job-details, the Hyperscale HA replica for job-orchestrator.
// - The job-orchestrator named replica is a different database name on the same server, selected by ReportingDatabase.
// Reporting without a ReportingDatabase falls back to the ReadOnly rule (effective role ReadOnly).
internal static class ConnectionStringRouter
{
    public static ConnectionRoute Resolve(DatabaseOptions options, DbRole role)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("DatabaseOptions.ConnectionString must be configured.");
        }

        return role switch
        {
            DbRole.ReadWrite => Build(role, DbRole.ReadWrite, options.ConnectionString, ApplicationIntent.ReadWrite, catalog: null),
            DbRole.ReadOnly => ResolveReadOnly(options, role),
            DbRole.Reporting when !string.IsNullOrWhiteSpace(options.ReportingDatabase)
                => Build(role, DbRole.Reporting, options.ConnectionString, ApplicationIntent.ReadOnly, options.ReportingDatabase),
            DbRole.Reporting => ResolveReadOnly(options, role),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };
    }

    private static ConnectionRoute ResolveReadOnly(DatabaseOptions options, DbRole requested) =>
        Build(requested, DbRole.ReadOnly, options.ConnectionString, ApplicationIntent.ReadOnly, catalog: null);

    private static ConnectionRoute Build(DbRole requested, DbRole effective, string baseString, ApplicationIntent intent, string? catalog)
    {
        var builder = new SqlConnectionStringBuilder(baseString) { ApplicationIntent = intent };
        if (catalog is not null)
        {
            builder.InitialCatalog = catalog;
        }

        return new ConnectionRoute(requested, effective, builder.DataSource, builder.InitialCatalog, intent, builder.ConnectionString);
    }
}
