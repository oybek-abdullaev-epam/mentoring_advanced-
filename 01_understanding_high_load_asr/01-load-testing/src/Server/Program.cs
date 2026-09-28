var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<RequestMetrics>();

var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapGet("/api/data", (int? size, RequestMetrics metrics) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var recordCount = Math.Min(size ?? 100, 10_000);
    var records = Enumerable.Range(1, recordCount)
        .Select(i => new JobRecord(i, $"Job-{i}", DateTime.UtcNow.AddMinutes(-i), "Completed", i * 10))
        .ToList();
    sw.Stop();
    metrics.Record(sw.ElapsedMilliseconds);
    return Results.Ok(new { count = records.Count, data = records, serverTimeMs = sw.ElapsedMilliseconds });
});

app.MapGet("/api/metrics/summary", (RequestMetrics metrics) => Results.Ok(metrics.GetSummary()));

app.Run();

record JobRecord(int Id, string Name, DateTime ExecutedAt, string Status, int DurationMs);

class RequestMetrics
{
    private readonly System.Collections.Concurrent.ConcurrentBag<long> _times = new();

    public void Record(long ms) => _times.Add(ms);

    public object GetSummary()
    {
        var sorted = _times.OrderBy(x => x).ToArray();
        if (sorted.Length == 0)
            return new { count = 0 };

        return new
        {
            count = sorted.Length,
            avgMs = sorted.Average(),
            p50Ms = sorted[(int)(sorted.Length * 0.50)],
            p95Ms = sorted[(int)(sorted.Length * 0.95)],
            p99Ms = sorted[(int)(sorted.Length * 0.99)],
            maxMs = sorted[^1]
        };
    }
}
