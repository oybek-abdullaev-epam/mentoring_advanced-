using System.Diagnostics;

namespace JobScheduler.Dal.IntegrationTests;

public static class Polling
{
    public static readonly TimeSpan ReplicaTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    // Polls until the probe returns a value accepted by isDone; Elapsed is the wait observed from the first probe.
    public static async Task<(T? Value, TimeSpan Elapsed)> UntilAsync<T>(
        Func<Task<T?>> probe, Func<T?, bool> isDone, CancellationToken ct, TimeSpan? timeout = null)
    {
        var limit = timeout ?? ReplicaTimeout;
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            var value = await probe();
            if (isDone(value) || stopwatch.Elapsed >= limit)
            {
                return (value, stopwatch.Elapsed);
            }

            await Task.Delay(Interval, ct);
        }
    }
}
