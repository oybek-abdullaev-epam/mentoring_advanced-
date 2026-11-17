using JobScheduler.Dal.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace JobScheduler.Dal.Connections;

public sealed partial class SqlConnectionFactory<TDatabase>(
    IOptions<JobSchedulerDalOptions> options,
    AzureSqlTokenProvider tokenProvider,
    TimeProvider timeProvider,
    ILogger<SqlConnectionFactory<TDatabase>> logger) : ISqlConnectionFactory<TDatabase>
    where TDatabase : IDatabaseMarker
{
    private readonly DatabaseOptions _options = SelectOptions(options.Value);

    // One delegate instance for the lifetime of the factory keeps the connection-pool key stable.
    private readonly Func<SqlAuthenticationParameters, CancellationToken, Task<SqlAuthenticationToken>> _tokenCallback =
        tokenProvider.GetTokenAsync;

    // One pipeline per role so the retry warning can name the role without per-call allocations.
    private readonly Dictionary<DbRole, ResiliencePipeline> _pipelines = Enum.GetValues<DbRole>().ToDictionary(
        role => role,
        role => SqlRetryPipeline.Create(
            options.Value.Retry,
            TransientSqlErrors.IsTransient,
            timeProvider,
            (ex, attempt, delay) => LogRetry(logger, TransientSqlErrors.FirstTransientNumber(ex), TDatabase.Name, role, attempt, options.Value.Retry.MaxAttempts, delay)));

    private static DatabaseOptions SelectOptions(JobSchedulerDalOptions all) =>
        TDatabase.Name == JobDetailsDb.Name ? all.JobDetails
        : TDatabase.Name == JobOrchestratorDb.Name ? all.JobOrchestrator
        : throw new InvalidOperationException($"Unknown database marker '{TDatabase.Name}'.");

    public async Task<SqlConnection> OpenAsync(DbRole role, CancellationToken ct = default)
    {
        var route = ConnectionStringRouter.Resolve(_options, role);
        var builder = new SqlConnectionStringBuilder(route.ConnectionString);

        var connection = new SqlConnection(route.ConnectionString);
        try
        {
            if (builder.Authentication == SqlAuthenticationMethod.NotSpecified && string.IsNullOrEmpty(builder.UserID))
            {
                connection.AccessTokenCallback = _tokenCallback;
            }

            await connection.OpenAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        LogOpened(logger, TDatabase.Name, route.RequestedRole, route.EffectiveRole, route.Intent, route.DataSource, route.Database);
        return connection;
    }

    public async Task<T> ExecuteAsync<T>(DbRole role, Func<SqlConnection, int, CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var attempt = 0;
        return await _pipelines[role].ExecuteAsync(async token =>
        {
            attempt++;
            await using var connection = await OpenAsync(role, token).ConfigureAwait(false);
            return await operation(connection, attempt, token).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Opened {Database} connection: requested={RequestedRole} effective={EffectiveRole} intent={Intent} target={DataSource}/{Catalog}")]
    private static partial void LogOpened(ILogger logger, string database, DbRole requestedRole, DbRole effectiveRole, ApplicationIntent intent, string dataSource, string catalog);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Transient SQL error {Number} on {Database} role={Role}, attempt {Attempt}/{MaxAttempts}, retrying in {Delay}")]
    private static partial void LogRetry(ILogger logger, int number, string database, DbRole role, int attempt, int maxAttempts, TimeSpan delay);
}
