using JobScheduler.Dal.Connections;
using JobScheduler.Dal.DependencyInjection;
using JobScheduler.Dal.JobDetails;
using JobScheduler.Dal.JobOrchestrator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(JobScheduler.Dal.IntegrationTests.AzureSqlFixture))]

namespace JobScheduler.Dal.IntegrationTests;

public sealed class AzureSqlFixture : IAsyncLifetime
{
    public const string NotConfiguredMessage = "Azure SQL not configured – follow infra/azure-sql/README.md (step 9)";

    private ServiceProvider? _services;

    // Both primaries are required: a half-configured setup would fail in the router instead of skipping.
    public bool NotConfigured { get; private set; } = true;

    public bool HasReportingDatabase { get; private set; }

    public ISqlConnectionFactory<JobDetailsDb> JobDetailsConnections => Required<ISqlConnectionFactory<JobDetailsDb>>();

    public ISqlConnectionFactory<JobOrchestratorDb> JobOrchestratorConnections => Required<ISqlConnectionFactory<JobOrchestratorDb>>();

    public IJobRepository Jobs => Required<IJobRepository>();

    public IJobScheduleSnapshotRepository Snapshots => Required<IJobScheduleSnapshotRepository>();

    public IJobRunRepository Runs => Required<IJobRunRepository>();

    public ValueTask InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(AzureSqlFixture).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? Value(string key) => configuration[$"JobSchedulerDal:{key}"];

        NotConfigured = string.IsNullOrWhiteSpace(Value("JobDetails:ConnectionString"))
            || string.IsNullOrWhiteSpace(Value("JobOrchestrator:ConnectionString"));
        if (NotConfigured)
        {
            return ValueTask.CompletedTask;
        }

        HasReportingDatabase = !string.IsNullOrWhiteSpace(Value("JobOrchestrator:ReportingDatabase"));

        // Building the provider opens no connection: factories connect lazily in OpenAsync.
        _services = new ServiceCollection()
            .AddLogging()
            .AddJobSchedulerDal(configuration)
            .BuildServiceProvider();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }
    }

    private T Required<T>() where T : notnull =>
        _services is null
            ? throw new InvalidOperationException("Azure SQL is not configured; the test must call Assert.SkipWhen(fixture.NotConfigured, ...) first.")
            : _services.GetRequiredService<T>();
}
