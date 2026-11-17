using System.Text.Json;
using Dapper;
using JobScheduler.Dal.Common;
using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;
using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.JobDetails;

public sealed class JobRepository(ISqlConnectionFactory<JobDetailsDb> connectionFactory, TimeProvider timeProvider) : IJobRepository
{
    private const string InsertJobSql = """
        INSERT INTO dbo.Job (Id, UserId, Name, Description, Status, ConcurrencyMode, Version, CreatedAt, UpdatedAt)
        VALUES (@Id, @UserId, @Name, @Description, @Status, @ConcurrencyMode, @Version, @CreatedAt, @UpdatedAt);
        """;

    private const string InsertScheduleSql = """
        INSERT INTO dbo.JobSchedule (JobId, ScheduleType, StartAt, RepeatStepSize, RepeatStepUnit)
        VALUES (@JobId, @ScheduleType, @StartAt, @RepeatStepSize, @RepeatStepUnit);
        """;

    private const string InsertLaunchDetailsSql = """
        INSERT INTO dbo.JobLaunchDetails (JobId, Endpoint, HttpMethod, Headers, Payload)
        VALUES (@JobId, @Endpoint, @HttpMethod, @Headers, @Payload);
        """;

    private const string InsertOutboxSql = """
        INSERT INTO dbo.JobOutbox (Id, JobId, EventType, JobVersion, Payload, CreatedAt)
        VALUES (@Id, @JobId, @EventType, @JobVersion, @Payload, @CreatedAt);
        """;

    private const string SelectViewSql = """
        SELECT j.Id, j.UserId, j.Name, j.Description, j.Status, j.ConcurrencyMode, j.Version, j.CreatedAt, j.UpdatedAt,
               s.ScheduleType, s.StartAt, s.RepeatStepSize, s.RepeatStepUnit,
               l.Endpoint, l.HttpMethod, l.Headers, l.Payload
        FROM dbo.Job j
        JOIN dbo.JobSchedule s ON s.JobId = j.Id
        JOIN dbo.JobLaunchDetails l ON l.JobId = j.Id
        """;

    private const string GetJobSql = SelectViewSql + "\nWHERE j.Id = @JobId;";

    private const string ListJobsByUserSql = SelectViewSql + "\nWHERE j.UserId = @UserId AND j.Status <> N'Deleted'\nORDER BY j.CreatedAt DESC;";

    // The CAS predicate is shared by update and soft delete: matching 0 rows means stale version, missing or already deleted.
    private const string UpdateJobSql = """
        UPDATE dbo.Job
        SET Name = @Name, Description = @Description, ConcurrencyMode = @ConcurrencyMode,
            Version = Version + 1, UpdatedAt = @UtcNow
        WHERE Id = @JobId AND Version = @ExpectedVersion AND Status <> N'Deleted';
        """;

    private const string SoftDeleteJobSql = """
        UPDATE dbo.Job
        SET Status = @Status, Version = Version + 1, UpdatedAt = @UtcNow
        WHERE Id = @JobId AND Version = @ExpectedVersion AND Status <> N'Deleted';
        """;

    private const string UpdateScheduleSql = """
        UPDATE dbo.JobSchedule
        SET ScheduleType = @ScheduleType, StartAt = @StartAt, RepeatStepSize = @RepeatStepSize, RepeatStepUnit = @RepeatStepUnit
        WHERE JobId = @JobId;
        """;

    private const string UpdateLaunchDetailsSql = """
        UPDATE dbo.JobLaunchDetails
        SET Endpoint = @Endpoint, HttpMethod = @HttpMethod, Headers = @Headers, Payload = @Payload
        WHERE JobId = @JobId;
        """;

    private const string OutboxExistsSql = "SELECT 1 FROM dbo.JobOutbox WHERE Id = @OutboxId;";

    private const string OutboxVersionSql = "SELECT JobVersion FROM dbo.JobOutbox WHERE Id = @OutboxId;";

    private const string GetJobUserIdSql = "SELECT UserId FROM dbo.Job WHERE Id = @JobId;";

    private const string GetPendingOutboxSql = """
        SELECT TOP (@BatchSize) Id, JobId, EventType, JobVersion, Payload, CreatedAt, ProcessedAt, Attempts
        FROM dbo.JobOutbox
        WHERE ProcessedAt IS NULL
        ORDER BY CreatedAt;
        """;

    // ProcessedAt IS NULL makes a repeat (a retry after a lost acknowledgement) a no-op instead of a second increment.
    private const string MarkOutboxProcessedSql = """
        UPDATE dbo.JobOutbox SET ProcessedAt = @UtcNow, Attempts = Attempts + 1 WHERE Id = @Id AND ProcessedAt IS NULL;
        """;

    private const string MarkOutboxFailedAttemptSql = """
        UPDATE dbo.JobOutbox SET Attempts = Attempts + 1 WHERE Id = @Id;
        """;

    static JobRepository() => DapperConfig.EnsureConfigured();

    public async Task<JobDetailsView> CreateJobAsync(NewJob job, CancellationToken ct)
    {
        ValidateNewJob(job);

        var utcNow = CurrentUtc();
        var view = new JobDetailsView
        {
            Id = Ids.NewId(),
            UserId = job.UserId,
            Name = job.Name,
            Description = job.Description,
            Status = JobStatus.Active,
            ConcurrencyMode = job.ConcurrencyMode,
            Version = 1,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            ScheduleType = job.Schedule.ScheduleType,
            StartAt = TruncateToMs(job.Schedule.StartAt),
            RepeatStepSize = job.Schedule.RepeatStepSize,
            RepeatStepUnit = job.Schedule.RepeatStepUnit,
            Endpoint = job.LaunchDetails.Endpoint,
            HttpMethod = job.LaunchDetails.HttpMethod,
            Headers = job.LaunchDetails.Headers,
            Payload = job.LaunchDetails.Payload,
        };

        // Both ids are fixed before the first attempt: after a lost commit acknowledgement the outbox row proves
        // the earlier attempt committed, and a retry would otherwise surface as a false DuplicateJobNameException.
        var outboxId = Ids.NewId();

        return await connectionFactory.ExecuteAsync(DbRole.ReadWrite, async (connection, attempt, token) =>
        {
            if (attempt > 1 && await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(OutboxExistsSql, new { OutboxId = outboxId }, cancellationToken: token)).ConfigureAwait(false) is not null)
            {
                return view;
            }

            await using var transaction = connection.BeginTransaction();
            try
            {
                await Execute(connection, transaction, InsertJobSql, new
                {
                    view.Id,
                    view.UserId,
                    view.Name,
                    view.Description,
                    Status = view.Status.ToString(),
                    ConcurrencyMode = view.ConcurrencyMode.ToString(),
                    view.Version,
                    view.CreatedAt,
                    view.UpdatedAt,
                }, token).ConfigureAwait(false);

                await Execute(connection, transaction, InsertScheduleSql, ScheduleParams(view.Id, view.ScheduleType, view.StartAt, view.RepeatStepSize, view.RepeatStepUnit), token).ConfigureAwait(false);
                await Execute(connection, transaction, InsertLaunchDetailsSql, LaunchParams(view.Id, view.Endpoint, view.HttpMethod, view.Headers, view.Payload), token).ConfigureAwait(false);
                await InsertOutboxAsync(connection, transaction, outboxId, view, OutboxEventType.JobCreated, utcNow, token).ConfigureAwait(false);

                await transaction.CommitAsync(token).ConfigureAwait(false);
            }
            catch (SqlException ex) when (IsUniqueViolation(ex))
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw new DuplicateJobNameException(job.UserId, job.Name, ex);
            }

            return view;
        }, ct).ConfigureAwait(false);
    }

    public async Task<JobDetailsView?> GetJobAsync(Guid jobId, ReadConsistency consistency = ReadConsistency.Eventual, CancellationToken ct = default)
    {
        return await connectionFactory.ExecuteAsync(consistency.ToRole(),
            (connection, _, token) => QueryViewAsync(connection, null, jobId, token), ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<JobDetailsView>> ListJobsByUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await connectionFactory.ExecuteAsync<IReadOnlyList<JobDetailsView>>(DbRole.ReadOnly, async (connection, _, token) =>
        {
            var rows = await connection.QueryAsync<JobDetailsView>(new CommandDefinition(ListJobsByUserSql, new { UserId = userId }, cancellationToken: token)).ConfigureAwait(false);
            return rows.AsList();
        }, ct).ConfigureAwait(false);
    }

    public async Task<int> UpdateJobAsync(UpdateJob cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        ValidateName(cmd.Name);
        ValidateSchedule(cmd.Schedule);
        ValidateLaunchDetails(cmd.LaunchDetails);

        var utcNow = CurrentUtc();
        var startAt = TruncateToMs(cmd.Schedule.StartAt);

        var outboxId = Ids.NewId();

        return await connectionFactory.ExecuteAsync(DbRole.ReadWrite, async (connection, attempt, token) =>
        {
            if (attempt > 1 && await FindCommittedVersionAsync(connection, outboxId, token).ConfigureAwait(false) is { } committedVersion)
            {
                return committedVersion;
            }

            await using var transaction = connection.BeginTransaction();
            try
            {
                var updated = await Execute(connection, transaction, UpdateJobSql, new
                {
                    cmd.JobId,
                    cmd.ExpectedVersion,
                    cmd.Name,
                    cmd.Description,
                    ConcurrencyMode = cmd.ConcurrencyMode.ToString(),
                    UtcNow = utcNow,
                }, token).ConfigureAwait(false);

                if (updated == 0)
                {
                    throw new ConcurrencyConflictException(cmd.JobId, cmd.ExpectedVersion);
                }

                await Execute(connection, transaction, UpdateScheduleSql, ScheduleParams(cmd.JobId, cmd.Schedule.ScheduleType, startAt, cmd.Schedule.RepeatStepSize, cmd.Schedule.RepeatStepUnit), token).ConfigureAwait(false);
                await Execute(connection, transaction, UpdateLaunchDetailsSql, LaunchParams(cmd.JobId, cmd.LaunchDetails.Endpoint, cmd.LaunchDetails.HttpMethod, cmd.LaunchDetails.Headers, cmd.LaunchDetails.Payload), token).ConfigureAwait(false);

                var version = await WriteOutboxFromCurrentStateAsync(connection, transaction, outboxId, cmd.JobId, OutboxEventType.JobUpdated, utcNow, token).ConfigureAwait(false);

                await transaction.CommitAsync(token).ConfigureAwait(false);
                return version;
            }
            catch (SqlException ex) when (IsUniqueViolation(ex))
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);

                // UpdateJob carries no user id, and the exception needs one: look it up after the rollback.
                var userId = await connection.QuerySingleOrDefaultAsync<Guid>(new CommandDefinition(GetJobUserIdSql, new { cmd.JobId }, cancellationToken: token)).ConfigureAwait(false);
                throw new DuplicateJobNameException(userId, cmd.Name, ex);
            }
        }, ct).ConfigureAwait(false);
    }

    public async Task<int> SoftDeleteJobAsync(Guid jobId, int expectedVersion, CancellationToken ct)
    {
        var utcNow = CurrentUtc();

        var outboxId = Ids.NewId();

        return await connectionFactory.ExecuteAsync(DbRole.ReadWrite, async (connection, attempt, token) =>
        {
            if (attempt > 1 && await FindCommittedVersionAsync(connection, outboxId, token).ConfigureAwait(false) is { } committedVersion)
            {
                return committedVersion;
            }

            await using var transaction = connection.BeginTransaction();

            var updated = await Execute(connection, transaction, SoftDeleteJobSql, new
            {
                JobId = jobId,
                ExpectedVersion = expectedVersion,
                Status = JobStatus.Deleted.ToString(),
                UtcNow = utcNow,
            }, token).ConfigureAwait(false);

            if (updated == 0)
            {
                throw new ConcurrencyConflictException(jobId, expectedVersion);
            }

            var version = await WriteOutboxFromCurrentStateAsync(connection, transaction, outboxId, jobId, OutboxEventType.JobDeleted, utcNow, token).ConfigureAwait(false);

            await transaction.CommitAsync(token).ConfigureAwait(false);
            return version;
        }, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<JobOutboxMessage>> GetPendingOutboxAsync(int batchSize, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);

        return await connectionFactory.ExecuteAsync<IReadOnlyList<JobOutboxMessage>>(DbRole.ReadWrite, async (connection, _, token) =>
        {
            var rows = await connection.QueryAsync<JobOutboxMessage>(new CommandDefinition(GetPendingOutboxSql, new { BatchSize = batchSize }, cancellationToken: token)).ConfigureAwait(false);
            return rows.AsList();
        }, ct).ConfigureAwait(false);
    }

    public async Task MarkOutboxProcessedAsync(Guid outboxId, CancellationToken ct)
    {
        var utcNow = CurrentUtc();
        await connectionFactory.ExecuteAsync(DbRole.ReadWrite,
            (connection, _, token) => connection.ExecuteAsync(new CommandDefinition(MarkOutboxProcessedSql, new { Id = outboxId, UtcNow = utcNow }, cancellationToken: token)),
            ct).ConfigureAwait(false);
    }

    public async Task MarkOutboxFailedAttemptAsync(Guid outboxId, CancellationToken ct)
    {
        await connectionFactory.ExecuteAsync(DbRole.ReadWrite,
            (connection, _, token) => connection.ExecuteAsync(new CommandDefinition(MarkOutboxFailedAttemptSql, new { Id = outboxId }, cancellationToken: token)),
            ct).ConfigureAwait(false);
    }

    // Re-selects inside the writing transaction (same connection, sees its own uncommitted changes) so the outbox
    // payload is exactly the row state that will be committed, including the bumped Version.
    private async Task<int> WriteOutboxFromCurrentStateAsync(
        SqlConnection connection, SqlTransaction transaction, Guid outboxId, Guid jobId, OutboxEventType eventType, DateTime utcNow, CancellationToken ct)
    {
        var view = await QueryViewAsync(connection, transaction, jobId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Job {jobId} disappeared inside its own transaction.");

        await InsertOutboxAsync(connection, transaction, outboxId, view, eventType, utcNow, ct).ConfigureAwait(false);
        return view.Version;
    }

    private static Task<JobDetailsView?> QueryViewAsync(SqlConnection connection, SqlTransaction? transaction, Guid jobId, CancellationToken ct) =>
        connection.QuerySingleOrDefaultAsync<JobDetailsView>(new CommandDefinition(GetJobSql, new { JobId = jobId }, transaction, cancellationToken: ct));

    // The outbox row is written in the same transaction as the change, so finding it means that attempt committed.
    private static Task<int?> FindCommittedVersionAsync(SqlConnection connection, Guid outboxId, CancellationToken ct) =>
        connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(OutboxVersionSql, new { OutboxId = outboxId }, cancellationToken: ct));

    private static Task InsertOutboxAsync(
        SqlConnection connection, SqlTransaction transaction, Guid outboxId, JobDetailsView view, OutboxEventType eventType, DateTime utcNow, CancellationToken ct) =>
        Execute(connection, transaction, InsertOutboxSql, new
        {
            Id = outboxId,
            JobId = view.Id,
            EventType = eventType.ToString(),
            JobVersion = view.Version,
            Payload = JsonSerializer.Serialize(JobPayloads.ToSnapshotPayload(view), Json.Default),
            CreatedAt = utcNow,
        }, ct);

    private static Task<int> Execute(SqlConnection connection, SqlTransaction transaction, string sql, object parameters, CancellationToken ct) =>
        connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction, cancellationToken: ct));

    private static object ScheduleParams(Guid jobId, ScheduleType scheduleType, DateTime startAt, int? repeatStepSize, RepeatStepUnit? repeatStepUnit) => new
    {
        JobId = jobId,
        ScheduleType = scheduleType.ToString(),
        StartAt = startAt,
        RepeatStepSize = repeatStepSize,
        RepeatStepUnit = repeatStepUnit?.ToString(),
    };

    private static object LaunchParams(Guid jobId, string endpoint, LaunchHttpMethod httpMethod, string? headers, string? payload) => new
    {
        JobId = jobId,
        Endpoint = endpoint,
        HttpMethod = httpMethod.ToString(),
        Headers = headers,
        Payload = payload,
    };

    private static bool IsUniqueViolation(SqlException ex) =>
        ex.Number is SqlErrorNumbers.UniqueIndexViolation or SqlErrorNumbers.UniqueConstraintViolation;

    private DateTime CurrentUtc() => TruncateToMs(timeProvider.GetUtcNow().UtcDateTime);

    // DATETIME2(3) rounds to the nearest millisecond on write; truncating up front makes the in-memory view equal the stored row.
    private static DateTime TruncateToMs(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, value.Kind);

    private static void ValidateNewJob(NewJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        ValidateName(job.Name);
        ValidateSchedule(job.Schedule);
        ValidateLaunchDetails(job.LaunchDetails);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Name must be at most 200 characters.", nameof(name));
        }
    }

    private static void ValidateSchedule(JobScheduleSpec schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        switch (schedule.ScheduleType)
        {
            case ScheduleType.Recurring:
                if (schedule.RepeatStepSize is not > 0 || schedule.RepeatStepUnit is null)
                {
                    throw new ArgumentException("A recurring schedule needs a positive RepeatStepSize and a RepeatStepUnit.", nameof(schedule));
                }

                break;
            case ScheduleType.OneTime:
                if (schedule.RepeatStepSize is not null || schedule.RepeatStepUnit is not null)
                {
                    throw new ArgumentException("A one-time schedule must not have RepeatStepSize or RepeatStepUnit.", nameof(schedule));
                }

                break;
            default:
                throw new ArgumentException($"Unknown schedule type '{schedule.ScheduleType}'.", nameof(schedule));
        }
    }

    private static void ValidateLaunchDetails(LaunchDetailsSpec launchDetails)
    {
        ArgumentNullException.ThrowIfNull(launchDetails);

        var isHttpUri = Uri.TryCreate(launchDetails.Endpoint, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        if (!isHttpUri)
        {
            throw new ArgumentException("Endpoint must be an absolute http or https URI.", nameof(launchDetails));
        }
    }
}
