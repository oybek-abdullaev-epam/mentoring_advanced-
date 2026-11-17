using Azure.Core;
using JobScheduler.Dal.Connections;
using JobScheduler.Dal.DependencyInjection;
using JobScheduler.Dal.JobDetails;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JobScheduler.Dal.UnitTests;

public class ServiceCollectionExtensionsTests
{
    private static IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JobSchedulerDal:JobDetails:ConnectionString"] = "Server=tcp:sql-x-eus.database.windows.net,1433;Database=job-details;Encrypt=True;",
            ["JobSchedulerDal:JobOrchestrator:ConnectionString"] = "Server=tcp:sql-x-eus.database.windows.net,1433;Database=job-orchestrator;Encrypt=True;",
            ["JobSchedulerDal:JobOrchestrator:ReportingDatabase"] = "job-orchestrator-reporting",
        })
        .Build();

    private sealed class FakeCredential : TokenCredential
    {
        public int Calls { get; private set; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            Calls++;
            return new("fake-token", DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(GetToken(requestContext, cancellationToken));
    }

    [Fact]
    public void BindsAllThreeConfigurationKeys()
    {
        using var provider = new ServiceCollection().AddLogging().AddJobSchedulerDal(Config()).BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<JobSchedulerDalOptions>>().Value;

        Assert.Contains("Database=job-details", options.JobDetails.ConnectionString);
        Assert.Contains("sql-x-eus.database.windows.net", options.JobOrchestrator.ConnectionString);
        Assert.Equal("job-orchestrator-reporting", options.JobOrchestrator.ReportingDatabase);
    }

    [Fact]
    public void RetryOptions_HaveDefaultValues()
    {
        using var provider = new ServiceCollection().AddLogging().AddJobSchedulerDal(Config()).BuildServiceProvider();

        var retry = provider.GetRequiredService<IOptions<JobSchedulerDalOptions>>().Value.Retry;

        Assert.Equal(5, retry.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(5), retry.FirstDelay);
        Assert.Equal(TimeSpan.FromSeconds(60), retry.MaxDelay);
    }

    [Fact]
    public void RetryOptions_CanBeOverriddenThroughConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobSchedulerDal:Retry:MaxAttempts"] = "2",
                ["JobSchedulerDal:Retry:FirstDelay"] = "00:00:01",
                ["JobSchedulerDal:Retry:MaxDelay"] = "00:00:03",
            })
            .Build();
        using var provider = new ServiceCollection().AddLogging().AddJobSchedulerDal(configuration).BuildServiceProvider();

        var retry = provider.GetRequiredService<IOptions<JobSchedulerDalOptions>>().Value.Retry;

        Assert.Equal(2, retry.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), retry.FirstDelay);
        Assert.Equal(TimeSpan.FromSeconds(3), retry.MaxDelay);
    }

    [Fact]
    public void ResolvesBothFactoriesWithoutOpeningAConnection()
    {
        using var provider = new ServiceCollection().AddLogging().AddJobSchedulerDal(Config()).BuildServiceProvider();

        var details = provider.GetRequiredService<ISqlConnectionFactory<JobDetailsDb>>();
        var orchestrator = provider.GetRequiredService<ISqlConnectionFactory<JobOrchestratorDb>>();

        Assert.IsType<SqlConnectionFactory<JobDetailsDb>>(details);
        Assert.IsType<SqlConnectionFactory<JobOrchestratorDb>>(orchestrator);
        Assert.Same(details, provider.GetRequiredService<ISqlConnectionFactory<JobDetailsDb>>());
    }

    [Fact]
    public void ResolvesJobRepositoryAsSingleton()
    {
        using var provider = new ServiceCollection().AddLogging().AddJobSchedulerDal(Config()).BuildServiceProvider();

        var repository = provider.GetRequiredService<IJobRepository>();

        Assert.IsType<JobRepository>(repository);
        Assert.Same(repository, provider.GetRequiredService<IJobRepository>());
    }

    [Fact]
    public void RegisteredTokenCredentialOverridesTheDefault()
    {
        var fake = new FakeCredential();
        var services = new ServiceCollection().AddLogging().AddSingleton<TokenCredential>(fake);

        using var provider = services.AddJobSchedulerDal(Config()).BuildServiceProvider();

        Assert.Same(fake, provider.GetRequiredService<TokenCredential>());
        Assert.NotNull(provider.GetRequiredService<ISqlConnectionFactory<JobDetailsDb>>());
    }

    [Fact]
    public async Task TokenProvider_UsesCredentialAndCachesToken()
    {
        var fake = new FakeCredential();
        using var provider = new AzureSqlTokenProvider(fake);

        // The parameters are not used by the provider; SqlAuthenticationParameters has no public test-friendly constructor.
        var first = await provider.GetTokenAsync(null!, CancellationToken.None);
        var second = await provider.GetTokenAsync(null!, CancellationToken.None);

        Assert.Equal("fake-token", first.AccessToken);
        Assert.Equal("fake-token", second.AccessToken);
        Assert.Equal(1, fake.Calls);
    }
}
