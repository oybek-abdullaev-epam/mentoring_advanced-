using JobScheduler.Dal.Common;
using JobScheduler.Dal.Connections;
using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.IntegrationTests;

public sealed class SimulatedTransientException : Exception;

// Simulates the ambiguous commit: attempt 1 runs the real operation (and commits), then the "connection drops" before
// the caller learns the result. Attempt 2 then goes through the repository's real reconciliation code.
public sealed class CommitThenFailConnectionFactory<TDatabase>(ISqlConnectionFactory<TDatabase> inner) : ISqlConnectionFactory<TDatabase>
    where TDatabase : IDatabaseMarker
{
    private static readonly SqlRetryOptions Options = new() { MaxAttempts = 3, FirstDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero };

    public Task<SqlConnection> OpenAsync(DbRole role, CancellationToken ct = default) => inner.OpenAsync(role, ct);

    public async Task<T> ExecuteAsync<T>(DbRole role, Func<SqlConnection, int, CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        var pipeline = SqlRetryPipeline.Create(
            Options, ex => ex is SimulatedTransientException || TransientSqlErrors.IsTransient(ex), TimeProvider.System, (_, _, _) => { });

        var attempt = 0;
        return await pipeline.ExecuteAsync(async token =>
        {
            attempt++;
            await using var connection = await inner.OpenAsync(role, token);
            var result = await operation(connection, attempt, token);
            return attempt == 1 ? throw new SimulatedTransientException() : result;
        }, ct);
    }
}
