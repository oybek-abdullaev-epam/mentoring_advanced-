namespace JobScheduler.Dal.Connections;

public interface IDatabaseMarker
{
    static abstract string Name { get; }
}

public sealed class JobDetailsDb : IDatabaseMarker
{
    public static string Name => "JobDetails";
}

public sealed class JobOrchestratorDb : IDatabaseMarker
{
    public static string Name => "JobOrchestrator";
}
