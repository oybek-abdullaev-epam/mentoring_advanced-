namespace JobScheduler.Dal.Common;

public static class Ids
{
    // SQL Server orders uniqueidentifier by its last bytes, so v7 GUIDs are effectively random on the clustered index.
    // That spreads inserts and avoids last-page latch contention at burst rate; the cost is page splits.
    public static Guid NewId() => Guid.CreateVersion7();
}
