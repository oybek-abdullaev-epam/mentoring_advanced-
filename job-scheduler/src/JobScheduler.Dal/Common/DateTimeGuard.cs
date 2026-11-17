namespace JobScheduler.Dal.Common;

internal static class DateTimeGuard
{
    // Unspecified is accepted: it is how SQL Server DATETIME2 values and parsed UTC strings arrive.
    public static void ThrowIfLocal(DateTime value, string paramName)
    {
        if (value.Kind == DateTimeKind.Local)
        {
            throw new ArgumentException("A local DateTime is not accepted: pass UTC (or Unspecified, treated as UTC).", paramName);
        }
    }
}
