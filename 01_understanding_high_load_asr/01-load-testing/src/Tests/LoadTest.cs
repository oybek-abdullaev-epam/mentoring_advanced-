using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

// Parse CLI arguments
string serverUrl = "http://localhost:5000";
string outputDir = "./metrics";

for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--server") serverUrl = args[i + 1];
    if (args[i] == "--output") outputDir = args[i + 1];
}

Directory.CreateDirectory(outputDir);

using var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };

// Scenarios: progressively increasing concurrency to observe scalability limits
var concurrencyLevels = new[] { 1, 10, 50, 100, 500 };
var allResults = new List<ScenarioResult>();

Console.WriteLine($"Starting load test against {serverUrl}");
Console.WriteLine($"Output directory: {outputDir}");
Console.WriteLine();

foreach (var concurrency in concurrencyLevels)
{
    Console.WriteLine($"--- Testing with {concurrency} concurrent users ---");

    var scenario = Scenario.Create($"load_test_{concurrency}_users", async context =>
    {
        var request = Http.CreateRequest("GET", $"{serverUrl}/api/data?size=100");
        var response = await Http.Send(httpClient, request);
        return response;
    })
    .WithWarmUpDuration(TimeSpan.FromSeconds(5))
    .WithLoadSimulations(
        Simulation.KeepConstant(copies: concurrency, during: TimeSpan.FromSeconds(30))
    );

    var result = NBomberRunner
        .RegisterScenarios(scenario)
        .WithoutReports()
        .Run();

    var stats = result.ScenarioStats.FirstOrDefault();
    if (stats != null)
    {
        var entry = new ScenarioResult(
            concurrency,
            stats.Ok.Request.Count,
            stats.Ok.Latency.MeanMs,
            stats.Ok.Latency.Percent95,
            stats.Ok.Latency.Percent99,
            stats.Ok.Request.RPS,
            stats.Fail.Request.Count,
            stats.Duration.TotalSeconds
        );
        allResults.Add(entry);

        Console.WriteLine($"  Requests: {entry.TotalRequests}  RPS: {entry.Rps:F1}  Avg: {entry.AvgMs:F1}ms  p95: {entry.P95Ms:F1}ms  Errors: {entry.FailedRequests}");
    }

    // Brief pause between scenarios to allow recovery
    await Task.Delay(TimeSpan.FromSeconds(10));
}

// Write CSV output
var csvPath = Path.Combine(outputDir, "load-test-results.csv");
await File.WriteAllLinesAsync(csvPath, new[]
{
    "concurrent_users,total_requests,avg_ms,p95_ms,p99_ms,rps,failed_requests,duration_s"
}.Concat(allResults.Select(r =>
    $"{r.ConcurrentUsers},{r.TotalRequests},{r.AvgMs:F2},{r.P95Ms:F2},{r.P99Ms:F2},{r.Rps:F2},{r.FailedRequests},{r.DurationS:F1}"
)));

Console.WriteLine();
Console.WriteLine($"Results written to: {csvPath}");
Console.WriteLine();

// Print summary table
Console.WriteLine("| Concurrent Users | Avg (ms) | p95 (ms) | p99 (ms) | RPS    | Errors |");
Console.WriteLine("|------------------|----------|----------|----------|--------|--------|");
foreach (var r in allResults)
{
    Console.WriteLine($"| {r.ConcurrentUsers,16} | {r.AvgMs,8:F1} | {r.P95Ms,8:F1} | {r.P99Ms,8:F1} | {r.Rps,6:F1} | {r.FailedRequests,6} |");
}

record ScenarioResult(
    int ConcurrentUsers,
    long TotalRequests,
    double AvgMs,
    double P95Ms,
    double P99Ms,
    double Rps,
    long FailedRequests,
    double DurationS
);
