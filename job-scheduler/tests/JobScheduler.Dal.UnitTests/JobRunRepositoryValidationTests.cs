using JobScheduler.Dal.Connections;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobOrchestrator;
using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.UnitTests;

public class JobRunRepositoryValidationTests
{
    private sealed class ConnectionRequestedException : Exception;

    private sealed class ThrowingConnectionFactory : ISqlConnectionFactory<JobOrchestratorDb>
    {
        public int OpenCalls { get; private set; }

        public Task<SqlConnection> OpenAsync(DbRole role, CancellationToken ct = default)
        {
            OpenCalls++;
            throw new ConnectionRequestedException();
        }

        public Task<T> ExecuteAsync<T>(DbRole role, Func<SqlConnection, int, CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            OpenCalls++;
            throw new ConnectionRequestedException();
        }
    }

    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(15);

    private static (JobRunRepository Repository, ThrowingConnectionFactory Factory) Create()
    {
        var factory = new ThrowingConnectionFactory();
        return (new JobRunRepository(factory, TimeProvider.System), factory);
    }

    public static TheoryData<JobRunStatus, JobRunStatus> DisallowedTransitions => new()
    {
        { JobRunStatus.Pending, JobRunStatus.Succeeded },
        { JobRunStatus.Pending, JobRunStatus.Failed },
        { JobRunStatus.Running, JobRunStatus.Pending },
        { JobRunStatus.Succeeded, JobRunStatus.Running },
        { JobRunStatus.Failed, JobRunStatus.Succeeded },
        { JobRunStatus.LockFailed, JobRunStatus.Running },
        { JobRunStatus.Running, JobRunStatus.Running },
    };

    [Theory]
    [MemberData(nameof(DisallowedTransitions))]
    public async Task TryUpdateStatus_DisallowedTransition_Throws_BeforeOpeningConnection(JobRunStatus expected, JobRunStatus next)
    {
        var (repository, factory) = Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.TryUpdateStatusAsync(Guid.NewGuid(), expected, next, Now, null, CancellationToken.None));

        Assert.Equal(0, factory.OpenCalls);
    }

    [Fact]
    public async Task TryUpdateStatus_AllowedTransition_ReachesTheConnection()
    {
        var (repository, factory) = Create();

        await Assert.ThrowsAsync<ConnectionRequestedException>(() =>
            repository.TryUpdateStatusAsync(Guid.NewGuid(), JobRunStatus.Pending, JobRunStatus.Running, Now, null, CancellationToken.None));

        Assert.Equal(1, factory.OpenCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public async Task ClaimDueJobs_BatchSizeOutOfRange_Throws_BeforeOpeningConnection(int batchSize)
    {
        var (repository, factory) = Create();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.ClaimDueJobsAsync(Now, batchSize, Lease, CancellationToken.None));

        Assert.Equal(0, factory.OpenCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public async Task ClaimDueJobs_NonPositiveLease_Throws_BeforeOpeningConnection(int leaseSeconds)
    {
        var (repository, factory) = Create();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.ClaimDueJobsAsync(Now, 10, TimeSpan.FromSeconds(leaseSeconds), CancellationToken.None));

        Assert.Equal(0, factory.OpenCalls);
    }

    [Fact]
    public async Task ClaimDueJobs_LocalUtcNow_Throws_BeforeOpeningConnection()
    {
        var (repository, factory) = Create();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.ClaimDueJobsAsync(DateTime.SpecifyKind(Now, DateTimeKind.Local), 10, Lease, CancellationToken.None));

        Assert.Equal(0, factory.OpenCalls);
    }

    [Fact]
    public async Task ClaimDueJobs_UnspecifiedUtcNow_ReachesTheConnection()
    {
        var (repository, _) = Create();

        await Assert.ThrowsAsync<ConnectionRequestedException>(() =>
            repository.ClaimDueJobsAsync(DateTime.SpecifyKind(Now, DateTimeKind.Unspecified), 10, Lease, CancellationToken.None));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public async Task ClaimDueJobs_BoundaryBatchSize_ReachesTheConnection(int batchSize)
    {
        var (repository, _) = Create();

        await Assert.ThrowsAsync<ConnectionRequestedException>(() =>
            repository.ClaimDueJobsAsync(Now, batchSize, Lease, CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public async Task GetRunHistory_TakeOutOfRange_Throws_BeforeOpeningConnection(int take)
    {
        var (repository, factory) = Create();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.GetRunHistoryAsync(Guid.NewGuid(), take, CancellationToken.None));

        Assert.Equal(0, factory.OpenCalls);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public async Task GetRunHistory_BoundaryTake_ReachesTheConnection(int take)
    {
        var (repository, _) = Create();

        await Assert.ThrowsAsync<ConnectionRequestedException>(() =>
            repository.GetRunHistoryAsync(Guid.NewGuid(), take, CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public async Task GetOverdueRuns_LimitOutOfRange_Throws_BeforeOpeningConnection(int limit)
    {
        var (repository, factory) = Create();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.GetOverdueRunsAsync(Now, limit, CancellationToken.None));

        Assert.Equal(0, factory.OpenCalls);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public async Task GetOverdueRuns_BoundaryLimit_ReachesTheConnection(int limit)
    {
        var (repository, _) = Create();

        await Assert.ThrowsAsync<ConnectionRequestedException>(() =>
            repository.GetOverdueRunsAsync(Now, limit, CancellationToken.None));
    }

    [Fact]
    public async Task GetPendingRunOutbox_NonPositiveBatchSize_Throws_BeforeOpeningConnection()
    {
        var (repository, factory) = Create();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.GetPendingRunOutboxAsync(0, CancellationToken.None));

        Assert.Equal(0, factory.OpenCalls);
    }
}
