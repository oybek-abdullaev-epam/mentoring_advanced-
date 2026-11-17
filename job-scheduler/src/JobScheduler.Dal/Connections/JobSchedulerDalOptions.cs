namespace JobScheduler.Dal.Connections;

public sealed class JobSchedulerDalOptions
{
    public const string SectionName = "JobSchedulerDal";

    public DatabaseOptions JobDetails { get; set; } = new();

    public DatabaseOptions JobOrchestrator { get; set; } = new();

    public SqlRetryOptions Retry { get; set; } = new();
}
