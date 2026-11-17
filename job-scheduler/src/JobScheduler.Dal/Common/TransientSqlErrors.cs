using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.Common;

internal static class TransientSqlErrors
{
    // Microsoft Learn "Troubleshoot common connection issues" and "Read queries on replicas", plus 1205 (deadlock
    // victim: re-running the whole transaction is the right reaction). -2 (client timeout) and 3906 are deliberately absent.
    private static readonly HashSet<int> Numbers =
    [
        4060, 40197, 40501, 40613, 49918, 49919, 49920, 4221, 615,
        1219, 3947, 3948, 3961,
        64, 233, 10053, 10054, 10060,
        1205,
    ];

    public static bool IsTransient(int number) => Numbers.Contains(number);

    public static bool IsTransient(Exception exception) =>
        exception is SqlException sql && (IsTransient(sql.Number) || sql.Errors.Cast<SqlError>().Any(e => IsTransient(e.Number)));

    public static int FirstTransientNumber(Exception exception) =>
        exception is SqlException sql
            ? sql.Errors.Cast<SqlError>().Select(e => e.Number).FirstOrDefault(IsTransient, sql.Number)
            : 0;
}
