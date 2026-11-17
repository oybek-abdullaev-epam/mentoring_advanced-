namespace JobScheduler.Dal.Connections;

public sealed class SqlRetryOptions
{
    public int MaxAttempts { get; set; } = 5;

    public TimeSpan FirstDelay { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(60);
}
