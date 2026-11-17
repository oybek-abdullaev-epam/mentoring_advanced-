using JobScheduler.Dal.Common;
using JobScheduler.Dal.Connections;
using Microsoft.Extensions.Time.Testing;

namespace JobScheduler.Dal.UnitTests;

public class SqlRetryPipelineTests
{
    private sealed class TransientException(string message = "transient") : Exception(message);

    private static readonly SqlRetryOptions Defaults = new();

    private sealed record Retry(Exception Error, int Attempt, TimeSpan Delay);

    private static (Polly.ResiliencePipeline Pipeline, FakeTimeProvider Time, List<Retry> Retries) Create(SqlRetryOptions? options = null)
    {
        var time = new FakeTimeProvider();
        var retries = new List<Retry>();
        var pipeline = SqlRetryPipeline.Create(
            options ?? Defaults, ex => ex is TransientException, time, (ex, attempt, delay) => retries.Add(new Retry(ex, attempt, delay)));
        return (pipeline, time, retries);
    }

    // Runs the task to completion, advancing the fake clock whenever it waits on a retry delay.
    private static async Task<T> Drive<T>(Task<T> task, FakeTimeProvider time)
    {
        while (!task.IsCompleted)
        {
            time.Advance(TimeSpan.FromSeconds(5));
            await Task.Yield();
        }

        return await task;
    }

    [Fact]
    public async Task TransientFailure_IsRetried_AndTheSuccessIsReturned()
    {
        var (pipeline, time, retries) = Create();
        var attempts = new List<int>();
        var calls = 0;

        var task = pipeline.ExecuteAsync(_ =>
        {
            attempts.Add(++calls);
            return calls < 3 ? throw new TransientException() : ValueTask.FromResult("ok");
        }, TestContext.Current.CancellationToken).AsTask();

        Assert.Equal("ok", await Drive(task, time));
        Assert.Equal([1, 2, 3], attempts);
        Assert.Equal([1, 2], retries.Select(r => r.Attempt));
    }

    [Fact]
    public async Task AfterMaxAttempts_TheOriginalExceptionIsRethrown()
    {
        var (pipeline, time, retries) = Create(new SqlRetryOptions { MaxAttempts = 3 });
        var calls = 0;
        var failure = new TransientException("last");

        var task = pipeline.ExecuteAsync<string>(_ =>
        {
            calls++;
            throw failure;
        }, TestContext.Current.CancellationToken).AsTask();

        var thrown = await Assert.ThrowsAsync<TransientException>(() => Drive(task, time));

        Assert.Same(failure, thrown);
        Assert.Equal(3, calls);
        Assert.Equal(2, retries.Count);
    }

    [Fact]
    public async Task NonTransientException_IsNotRetried()
    {
        var (pipeline, _, retries) = Create();
        var calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pipeline.ExecuteAsync<string>(_ =>
        {
            calls++;
            throw new InvalidOperationException();
        }, TestContext.Current.CancellationToken));

        Assert.Equal(1, calls);
        Assert.Empty(retries);
    }

    [Fact]
    public async Task MaxAttemptsOne_DisablesRetry()
    {
        var (pipeline, _, retries) = Create(new SqlRetryOptions { MaxAttempts = 1 });
        var calls = 0;

        await Assert.ThrowsAsync<TransientException>(async () => await pipeline.ExecuteAsync<string>(_ =>
        {
            calls++;
            throw new TransientException();
        }, TestContext.Current.CancellationToken));

        Assert.Equal(1, calls);
        Assert.Empty(retries);
    }

    [Fact]
    public async Task CancelledToken_StopsRetrying()
    {
        var (pipeline, _, _) = Create();
        using var cts = new CancellationTokenSource();
        var calls = 0;

        // Polly notices the cancellation when it starts the delay, so no second attempt runs.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pipeline.ExecuteAsync<string>(_ =>
        {
            calls++;
            cts.Cancel();
            throw new TransientException();
        }, cts.Token));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CancellationDuringDelay_StopsRetrying()
    {
        var (pipeline, time, retries) = Create();
        using var cts = new CancellationTokenSource();
        var calls = 0;

        var task = pipeline.ExecuteAsync<string>(_ =>
        {
            calls++;
            throw new TransientException();
        }, cts.Token).AsTask();

        while (retries.Count == 0)
        {
            await Task.Yield();
        }

        cts.Cancel();
        time.Advance(TimeSpan.FromMinutes(5));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Delays_GrowExponentially_AndNeverExceedTheCap()
    {
        var options = new SqlRetryOptions { MaxAttempts = 8, FirstDelay = TimeSpan.FromSeconds(5), MaxDelay = TimeSpan.FromSeconds(60) };
        var (pipeline, time, retries) = Create(options);

        var task = pipeline.ExecuteAsync<string>(_ => throw new TransientException(), TestContext.Current.CancellationToken).AsTask();
        await Assert.ThrowsAsync<TransientException>(() => Drive(task, time));

        Assert.Equal(7, retries.Count);
        Assert.All(retries, r => Assert.InRange(r.Delay, TimeSpan.Zero, options.MaxDelay));
        Assert.True(retries[0].Delay <= TimeSpan.FromSeconds(10), "first delay is about FirstDelay");
        Assert.True(retries[^1].Delay >= TimeSpan.FromSeconds(30), "late delays approach the cap");
    }

    [Theory]
    [InlineData(0, 5, 60)]
    [InlineData(-1, 5, 60)]
    public void InvalidAttempts_Throw(int maxAttempts, int firstSeconds, int maxSeconds) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(new SqlRetryOptions
        {
            MaxAttempts = maxAttempts,
            FirstDelay = TimeSpan.FromSeconds(firstSeconds),
            MaxDelay = TimeSpan.FromSeconds(maxSeconds),
        }));

    [Fact]
    public void NegativeDelays_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(new SqlRetryOptions { FirstDelay = TimeSpan.FromSeconds(-1) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(new SqlRetryOptions { MaxDelay = TimeSpan.FromSeconds(-1) }));
    }
}
