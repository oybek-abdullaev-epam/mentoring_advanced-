using System.Data;
using Dapper;

namespace JobScheduler.Dal.Common;

internal static class DapperConfig
{
    // Dapper sends DateTime as legacy datetime by default, which rounds to 3.33 ms; the columns are DATETIME2(3).
    // The CLR runs a static constructor exactly once and thread-safely, so no lock or flag is needed.
    static DapperConfig()
    {
        SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2);
        SqlMapper.AddTypeMap(typeof(DateTime?), DbType.DateTime2);
    }

    public static void EnsureConfigured()
    {
        // Intentionally empty: calling it forces the static constructor above to have run.
    }
}
