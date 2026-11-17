using System.Text.Json;
using Dapper;
using JobScheduler.Dal.Common;
using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;
using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.JobOrchestrator;

public sealed class JobRunRepository(ISqlConnectionFactory<JobOrchestratorDb> connectionFactory, TimeProvider timeProvider) : IJobRunRepository
{
    private const int MaxClaimBatchSize = 1000;
    private const int MaxHistoryTake = 500;
    private const int MaxOverdueLimit = 1000;

    // UPDLOCK keeps the selected rows locked until commit, so no other claimer can pick (or update) them meanwhile.
    // READPAST makes a parallel claimer skip rows locked by someone else instead of blocking on them or claiming
    // them twice. ROWLOCK keeps the lock granularity at single rows, so claimers do not block each other on pages.
    private const string SelectDueSql = """
        SELECT TOP (@BatchSize) JobId, ConcurrencyMode, ScheduleType, RepeatStepSize, RepeatStepUnit,
               NextRunAt, LaunchDetails, CurrentRunId, LockedUntil
        FROM dbo.JobScheduleSnapshot WITH (UPDLOCK, READPAST, ROWLOCK)
        WHERE Status = N'Active' AND NextRunAt <= @UtcNow
        ORDER BY NextRunAt;
        """;

    private const string DeactivateSnapshotSql = """
        UPDATE dbo.JobScheduleSnapshot SET Status = N'Inactive' WHERE JobId = @JobId;
        """;

    private const string AdvanceNextRunSql = """
        UPDATE dbo.JobScheduleSnapshot SET NextRunAt = @NextRunAt WHERE JobId = @JobId;
        """;

    // Free, or held by a run whose lease has expired (the owner crashed without releasing).
    private const string TakeLockSql = """
        UPDATE dbo.JobScheduleSnapshot
        SET CurrentRunId = @RunId, LockedUntil = @LockedUntil
        WHERE JobId = @JobId AND (CurrentRunId IS NULL OR LockedUntil < @UtcNow);
        """;

    private const string InsertRunSql = """
        INSERT INTO dbo.JobRun (Id, JobId, ScheduledFor, Status, Deadline, CreatedAt)
        SELECT @Id, @JobId, @ScheduledFor, N'Pending', @Deadline, @CreatedAt
        WHERE NOT EXISTS (SELECT 1 FROM dbo.JobRun WHERE JobId = @JobId AND ScheduledFor = @ScheduledFor);
        """;

    private const string MarkLockFailedSql = """
        UPDATE dbo.JobRun
        SET Status = N'LockFailed', Deadline = NULL, FinishedAt = @UtcNow, ErrorSummary = @ErrorSummary
        WHERE Id = @RunId;
        """;

    private const string InsertRunOutboxSql = """
        INSERT INTO dbo.JobRunOutbox (Id, RunId, EventType, Payload, ScheduledEnqueueAt, CreatedAt)
        VALUES (@Id, @RunId, @EventType, @Payload, @ScheduledEnqueueAt, @CreatedAt);
        """;

    private const string UpdateStatusSql = """
        UPDATE dbo.JobRun
        SET Status = @Next,
            StartedAt = CASE WHEN @Next = N'Running' THEN @UtcNow ELSE StartedAt END,
            FinishedAt = CASE WHEN @Next IN (N'Succeeded', N'Failed', N'TimedOut', N'Cancelled') THEN @UtcNow ELSE FinishedAt END,
            ErrorSummary = COALESCE(@ErrorSummary, ErrorSummary)
        WHERE Id = @RunId AND Status = @Expected;
        """;

    private const string GetRunStatusSql = """
        SELECT Status FROM dbo.JobRun WHERE Id = @RunId;
        """;

    private const string ReleaseLockSql = """
        UPDATE dbo.JobScheduleSnapshot SET CurrentRunId = NULL, LockedUntil = NULL
        WHERE JobId = @JobId AND CurrentRunId = @RunId;
        """;

    private const string SetSequenceNumberSql = """
        UPDATE dbo.JobRun SET ServiceBusSequenceNumber = @SequenceNumber WHERE Id = @RunId;
        """;

    private const string GetPendingRunOutboxSql = """
        SELECT TOP (@BatchSize) Id, RunId, EventType, Payload, ScheduledEnqueueAt, CreatedAt, ProcessedAt, Attempts
        FROM dbo.JobRunOutbox
        WHERE ProcessedAt IS NULL
        ORDER BY CreatedAt;
        """;

    private const string MarkRunOutboxProcessedSql = """
        UPDATE dbo.JobRunOutbox SET ProcessedAt = @UtcNow, Attempts = Attempts + 1 WHERE Id = @Id AND ProcessedAt IS NULL;
        """;

    private const string SelectRunSql = """
        SELECT Id, JobId, ScheduledFor, Status, ServiceBusSequenceNumber, Deadline, StartedAt, FinishedAt, ErrorSummary, CreatedAt
        FROM dbo.JobRun
        """;

    private const string GetRunSql = SelectRunSql + "\nWHERE Id = @RunId;";

    private const string GetRunHistorySql = """
        SELECT TOP (@Take) Id, JobId, ScheduledFor, Status, ServiceBusSequenceNumber, Deadline, StartedAt, FinishedAt, ErrorSummary, CreatedAt
        FROM dbo.JobRun
        WHERE JobId = @JobId
        ORDER BY ScheduledFor DESC;
        """;

    // LockFailed runs have no Deadline, so they never match. Served by IX_JobRun_Overdue.
    private const string GetOverdueRunsSql = """
        SELECT TOP (@Limit) Id, JobId, ScheduledFor, Status, ServiceBusSequenceNumber, Deadline, StartedAt, FinishedAt, ErrorSummary, CreatedAt
        FROM dbo.JobRun
        WHERE Status IN (N'Pending', N'Running') AND Deadline < @UtcNow
        ORDER BY Deadline;
        """;

    static JobRunRepository() => DapperConfig.EnsureConfigured();

    public async Task<IReadOnlyList<ClaimedRun>> ClaimDueJobsAsync(DateTime utcNow, int batchSize, TimeSpan runLease, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(batchSize, MaxClaimBatchSize);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(runLease, TimeSpan.Zero);
        DateTimeGuard.ThrowIfLocal(utcNow, nameof(utcNow));

        return await connectionFactory.ExecuteAsync<IReadOnlyList<ClaimedRun>>(DbRole.ReadWrite, async (connection, _, token) =>
        {
            await using var transaction = connection.BeginTransaction();

            var due = (await connection.QueryAsync<DueSnapshot>(
                new CommandDefinition(SelectDueSql, new { BatchSize = batchSize, UtcNow = utcNow }, transaction, cancellationToken: token)).ConfigureAwait(false)).AsList();

            var claimed = new List<ClaimedRun>(due.Count);
            foreach (var row in due)
            {
                if (await ClaimAsync(connection, transaction, row, utcNow, runLease, token).ConfigureAwait(false) is { } run)
                {
                    claimed.Add(run);
                }
            }

            await transaction.CommitAsync(token).ConfigureAwait(false);
            return claimed;
        }, ct).ConfigureAwait(false);
    }

    public async Task<bool> TryUpdateStatusAsync(Guid runId, JobRunStatus expected, JobRunStatus next, DateTime utcNow, string? errorSummary, CancellationToken ct)
    {
        if (!JobRunStatusTransitions.IsAllowed(expected, next))
        {
            throw new InvalidOperationException($"Run status transition {expected} -> {next} is not allowed.");
        }

        var parameters = new
        {
            RunId = runId,
            Expected = expected.ToString(),
            Next = next.ToString(),
            UtcNow = utcNow,
            ErrorSummary = errorSummary,
        };

        return await connectionFactory.ExecuteAsync(DbRole.ReadWrite, async (connection, _, token) =>
        {
            var rows = await connection.ExecuteAsync(new CommandDefinition(UpdateStatusSql, parameters, cancellationToken: token)).ConfigureAwait(false);
            if (rows == 1)
            {
                return true;
            }

            // 0 rows: someone else moved the run, or an earlier attempt of ours committed before the connection dropped.
            // This is a ReadWrite connection, so the re-read hits the primary and cannot be stale.
            var current = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(GetRunStatusSql, new { RunId = runId }, cancellationToken: token)).ConfigureAwait(false);
            return current == parameters.Next;
        }, ct).ConfigureAwait(false);
    }

    public async Task ReleaseLockAsync(Guid jobId, Guid runId, CancellationToken ct)
    {
        // 0 rows means runId no longer holds the lock (released by us, an earlier attempt, or taken over after the lease expired).
        await connectionFactory.ExecuteAsync(DbRole.ReadWrite,
            (connection, _, token) => connection.ExecuteAsync(new CommandDefinition(ReleaseLockSql, new { JobId = jobId, RunId = runId }, cancellationToken: token)),
            ct).ConfigureAwait(false);
    }

    public async Task SetServiceBusSequenceNumberAsync(Guid runId, long sequenceNumber, CancellationToken ct)
    {
        await connectionFactory.ExecuteAsync(DbRole.ReadWrite,
            (connection, _, token) => connection.ExecuteAsync(new CommandDefinition(SetSequenceNumberSql, new { RunId = runId, SequenceNumber = sequenceNumber }, cancellationToken: token)),
            ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RunOutboxMessage>> GetPendingRunOutboxAsync(int batchSize, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);

        return await connectionFactory.ExecuteAsync<IReadOnlyList<RunOutboxMessage>>(DbRole.ReadWrite, async (connection, _, token) =>
        {
            var rows = await connection.QueryAsync<RunOutboxMessage>(new CommandDefinition(GetPendingRunOutboxSql, new { BatchSize = batchSize }, cancellationToken: token)).ConfigureAwait(false);
            return rows.AsList();
        }, ct).ConfigureAwait(false);
    }

    public async Task MarkRunOutboxProcessedAsync(Guid outboxId, CancellationToken ct)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        await connectionFactory.ExecuteAsync(DbRole.ReadWrite,
            (connection, _, token) => connection.ExecuteAsync(new CommandDefinition(MarkRunOutboxProcessedSql, new { Id = outboxId, UtcNow = utcNow }, cancellationToken: token)),
            ct).ConfigureAwait(false);
    }

    public async Task<JobRunView?> GetRunAsync(Guid runId, ReadConsistency consistency = ReadConsistency.Eventual, CancellationToken ct = default)
    {
        return await connectionFactory.ExecuteAsync(consistency.ToRole(),
            (connection, _, token) => connection.QuerySingleOrDefaultAsync<JobRunView>(new CommandDefinition(GetRunSql, new { RunId = runId }, cancellationToken: token)),
            ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<JobRunView>> GetRunHistoryAsync(Guid jobId, int take, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(take, MaxHistoryTake);

        return await connectionFactory.ExecuteAsync<IReadOnlyList<JobRunView>>(DbRole.Reporting, async (connection, _, token) =>
        {
            var rows = await connection.QueryAsync<JobRunView>(new CommandDefinition(GetRunHistorySql, new { JobId = jobId, Take = take }, cancellationToken: token)).ConfigureAwait(false);
            return rows.AsList();
        }, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<JobRunView>> GetOverdueRunsAsync(DateTime utcNow, int limit, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, MaxOverdueLimit);

        return await connectionFactory.ExecuteAsync<IReadOnlyList<JobRunView>>(DbRole.ReadOnly, async (connection, _, token) =>
        {
            var rows = await connection.QueryAsync<JobRunView>(new CommandDefinition(GetOverdueRunsSql, new { Limit = limit, UtcNow = utcNow }, cancellationToken: token)).ConfigureAwait(false);
            return rows.AsList();
        }, ct).ConfigureAwait(false);
    }

    // Claims one locked snapshot row: advance (or deactivate) its schedule, record the run as Pending, then take the
    // Singleton lock for it and, unless the lock was lost, write its ExecuteJob outbox message. All on the claim transaction.
    // The lock is taken only after the run exists, so a run skipped as already claimed never leaves a lock behind.
    // Returns null when a run for this window already exists: one bad row must not fail the whole batch.
    private static async Task<ClaimedRun?> ClaimAsync(
        SqlConnection connection, SqlTransaction transaction, DueSnapshot row, DateTime utcNow, TimeSpan runLease, CancellationToken ct)
    {
        var runId = Ids.NewId();
        var scheduledFor = row.NextRunAt;

        // Recurring jobs jump to the first occurrence after now: missed windows are skipped, not back-filled.
        DateTime? nextRunAt = null;
        if (row.ScheduleType == ScheduleType.OneTime)
        {
            await Execute(connection, transaction, DeactivateSnapshotSql, new { row.JobId }, ct).ConfigureAwait(false);
        }
        else
        {
            if (row.RepeatStepSize is not { } step || row.RepeatStepUnit is not { } unit)
            {
                throw new InvalidOperationException($"Recurring snapshot {row.JobId} has no repeat step.");
            }

            nextRunAt = NextRunCalculator.Next(scheduledFor, step, unit, utcNow);
            await Execute(connection, transaction, AdvanceNextRunSql, new { row.JobId, NextRunAt = nextRunAt }, ct).ConfigureAwait(false);
        }

        var deadline = utcNow + runLease;
        var inserted = await Execute(connection, transaction, InsertRunSql, new
        {
            Id = runId,
            row.JobId,
            ScheduledFor = scheduledFor,
            Deadline = deadline,
            CreatedAt = utcNow,
        }, ct).ConfigureAwait(false);

        if (inserted == 0)
        {
            return null;
        }

        var status = JobRunStatus.Pending;
        if (row.ConcurrencyMode == ConcurrencyMode.Singleton)
        {
            var locked = await Execute(connection, transaction, TakeLockSql, new { row.JobId, RunId = runId, LockedUntil = deadline, UtcNow = utcNow }, ct).ConfigureAwait(false);
            if (locked == 0)
            {
                status = JobRunStatus.LockFailed;
                await Execute(connection, transaction, MarkLockFailedSql, new
                {
                    RunId = runId,
                    UtcNow = utcNow,
                    ErrorSummary = $"Singleton lock held by run {row.CurrentRunId}",
                }, ct).ConfigureAwait(false);
            }
        }

        if (status == JobRunStatus.Pending)
        {
            var launchDetails = JsonSerializer.Deserialize<LaunchDetailsPayload>(row.LaunchDetails, Json.Default)
                ?? throw new InvalidOperationException($"Snapshot {row.JobId} has empty launch details.");
            var payload = new ExecuteJobPayload
            {
                RunId = runId,
                JobId = row.JobId,
                ScheduledFor = scheduledFor,
                Deadline = deadline,
                LaunchDetails = launchDetails,
            };

            await Execute(connection, transaction, InsertRunOutboxSql, new
            {
                Id = Ids.NewId(),
                RunId = runId,
                EventType = RunOutboxEventType.ExecuteJob.ToString(),
                Payload = JsonSerializer.Serialize(payload, Json.Default),
                ScheduledEnqueueAt = scheduledFor,
                CreatedAt = utcNow,
            }, ct).ConfigureAwait(false);
        }

        return new ClaimedRun
        {
            RunId = runId,
            JobId = row.JobId,
            ScheduledFor = scheduledFor,
            Status = status,
            NextRunAt = nextRunAt,
        };
    }

    private static Task<int> Execute(SqlConnection connection, SqlTransaction transaction, string sql, object parameters, CancellationToken ct) =>
        connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction, cancellationToken: ct));

    private sealed class DueSnapshot
    {
        public Guid JobId { get; init; }
        public ConcurrencyMode ConcurrencyMode { get; init; }
        public ScheduleType ScheduleType { get; init; }
        public int? RepeatStepSize { get; init; }
        public RepeatStepUnit? RepeatStepUnit { get; init; }
        public DateTime NextRunAt { get; init; }
        public string LaunchDetails { get; init; } = "";
        public Guid? CurrentRunId { get; init; }
        public DateTime? LockedUntil { get; init; }
    }
}
