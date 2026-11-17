namespace JobScheduler.Dal.Connections;

public enum ReadConsistency
{
    Eventual,
    Strong,
}

public static class ReadConsistencyExtensions
{
    public static DbRole ToRole(this ReadConsistency consistency) => consistency switch
    {
        ReadConsistency.Strong => DbRole.ReadWrite,
        ReadConsistency.Eventual => DbRole.ReadOnly,
        _ => throw new ArgumentOutOfRangeException(nameof(consistency), consistency, null),
    };
}
