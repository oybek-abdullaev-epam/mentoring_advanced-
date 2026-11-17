using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.Connections;

public interface ISqlConnectionFactory<TDatabase> where TDatabase : IDatabaseMarker
{
    Task<SqlConnection> OpenAsync(DbRole role, CancellationToken ct = default);

    // Runs the whole unit of work on a fresh connection per attempt and retries it on transient SQL errors.
    // The int is the 1-based attempt number: operations use it to reconcile an earlier, possibly committed attempt.
    Task<T> ExecuteAsync<T>(DbRole role, Func<SqlConnection, int, CancellationToken, Task<T>> operation, CancellationToken ct = default);
}

public static class SqlConnectionFactoryExtensions
{
    public static Task ExecuteAsync<TDatabase>(
        this ISqlConnectionFactory<TDatabase> factory,
        DbRole role,
        Func<SqlConnection, int, CancellationToken, Task> operation,
        CancellationToken ct = default)
        where TDatabase : IDatabaseMarker =>
        factory.ExecuteAsync<object?>(role, async (connection, attempt, token) =>
        {
            await operation(connection, attempt, token).ConfigureAwait(false);
            return null;
        }, ct);
}
