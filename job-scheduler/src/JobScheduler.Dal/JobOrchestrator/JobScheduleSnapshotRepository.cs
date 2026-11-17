using Dapper;
using JobScheduler.Dal.Common;
using JobScheduler.Dal.Connections;
using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.JobOrchestrator;

public sealed class JobScheduleSnapshotRepository(ISqlConnectionFactory<JobOrchestratorDb> connectionFactory) : IJobScheduleSnapshotRepository
{
    // A one-time job that already fired at this StartAt stays Inactive, otherwise the claim would hit
    // UQ_JobRun_JobId_ScheduledFor. Moving StartAt to a new time still reactivates it. Only an Active status is
    // downgraded, so a Deleted snapshot is never overwritten.
    private const string UpdateIfNewerSql = """
        UPDATE dbo.JobScheduleSnapshot
        SET ConcurrencyMode = @ConcurrencyMode, ScheduleType = @ScheduleType,
            RepeatStepSize = @RepeatStepSize, RepeatStepUnit = @RepeatStepUnit,
            NextRunAt = @NextRunAt, LaunchDetails = @LaunchDetails,
            Status = CASE WHEN @ScheduleType = N'OneTime' AND @Status = N'Active'
                           AND EXISTS (SELECT 1 FROM dbo.JobRun WHERE JobId = @JobId AND ScheduledFor = @NextRunAt)
                          THEN N'Inactive' ELSE @Status END,
            Version = @Version
        WHERE JobId = @JobId AND Version < @Version;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.JobScheduleSnapshot
            (JobId, UserId, ConcurrencyMode, ScheduleType, RepeatStepSize, RepeatStepUnit, NextRunAt, LaunchDetails, Status, Version, CreatedAt)
        SELECT @JobId, @UserId, @ConcurrencyMode, @ScheduleType, @RepeatStepSize, @RepeatStepUnit, @NextRunAt, @LaunchDetails, @Status, @Version, @CreatedAt
        WHERE NOT EXISTS (SELECT 1 FROM dbo.JobScheduleSnapshot WHERE JobId = @JobId);
        """;

    private const string GetSql = """
        SELECT JobId, UserId, ConcurrencyMode, ScheduleType, RepeatStepSize, RepeatStepUnit, NextRunAt,
               LaunchDetails, Status, Version, CreatedAt, CurrentRunId, LockedUntil
        FROM dbo.JobScheduleSnapshot
        WHERE JobId = @JobId;
        """;

    static JobScheduleSnapshotRepository() => DapperConfig.EnsureConfigured();

    // Two statements, each atomic, no explicit transaction. UPDATE first because redeliveries and edits are the
    // common case; INSERT only when no row matched. The INSERT is conditional, so an existing (stale) row inserts
    // 0 rows and reports IgnoredStale without an exception. Only a concurrent insert race still hits the primary
    // key (2627), and then the UPDATE is retried once: it either applies a newer version or reports it stale.
    public async Task<SnapshotUpsertResult> UpsertAsync(JobScheduleSnapshot snapshot, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var parameters = new
        {
            snapshot.JobId,
            snapshot.UserId,
            ConcurrencyMode = snapshot.ConcurrencyMode.ToString(),
            ScheduleType = snapshot.ScheduleType.ToString(),
            snapshot.RepeatStepSize,
            RepeatStepUnit = snapshot.RepeatStepUnit?.ToString(),
            snapshot.NextRunAt,
            snapshot.LaunchDetails,
            Status = snapshot.Status.ToString(),
            snapshot.Version,
            snapshot.CreatedAt,
        };

        // The version guard makes a retry safe: if an earlier attempt committed, this one reports IgnoredStale ("already applied").
        return await connectionFactory.ExecuteAsync(DbRole.ReadWrite, async (connection, _, token) =>
        {
            if (await UpdateIfNewerAsync(connection, parameters, token).ConfigureAwait(false) == 1)
            {
                return SnapshotUpsertResult.Updated;
            }

            try
            {
                var inserted = await connection.ExecuteAsync(new CommandDefinition(InsertSql, parameters, cancellationToken: token)).ConfigureAwait(false);
                return inserted == 1 ? SnapshotUpsertResult.Inserted : SnapshotUpsertResult.IgnoredStale;
            }
            catch (SqlException ex) when (ex.Number == SqlErrorNumbers.UniqueConstraintViolation)
            {
                return await UpdateIfNewerAsync(connection, parameters, token).ConfigureAwait(false) == 1
                    ? SnapshotUpsertResult.Updated
                    : SnapshotUpsertResult.IgnoredStale;
            }
        }, ct).ConfigureAwait(false);
    }

    public async Task<JobScheduleSnapshot?> GetAsync(Guid jobId, ReadConsistency consistency = ReadConsistency.Eventual, CancellationToken ct = default)
    {
        return await connectionFactory.ExecuteAsync(consistency.ToRole(),
            (connection, _, token) => connection.QuerySingleOrDefaultAsync<JobScheduleSnapshot>(new CommandDefinition(GetSql, new { JobId = jobId }, cancellationToken: token)),
            ct).ConfigureAwait(false);
    }

    private static Task<int> UpdateIfNewerAsync(SqlConnection connection, object parameters, CancellationToken ct) =>
        connection.ExecuteAsync(new CommandDefinition(UpdateIfNewerSql, parameters, cancellationToken: ct));
}
