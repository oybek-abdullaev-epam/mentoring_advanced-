namespace JobScheduler.Dal.IntegrationTests;

public abstract class IntegrationTestBase(AzureSqlFixture fixture, ITestOutputHelper output) : IAsyncLifetime
{
    protected AzureSqlFixture Fixture { get; } = fixture;

    protected ITestOutputHelper Output { get; } = output;

    protected TestDataCleaner Cleaner { get; } = new(fixture);

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => Cleaner.CleanAsync();
}
