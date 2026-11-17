using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.Connections;

public sealed record ConnectionRoute(
    DbRole RequestedRole,
    DbRole EffectiveRole,
    string DataSource,
    string Database,
    ApplicationIntent Intent,
    string ConnectionString);
