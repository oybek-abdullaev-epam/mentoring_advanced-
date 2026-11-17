namespace JobScheduler.Dal.Connections;

public sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = "";

    public string? ReportingDatabase { get; set; }
}
