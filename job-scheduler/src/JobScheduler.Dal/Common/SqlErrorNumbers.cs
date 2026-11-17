namespace JobScheduler.Dal.Common;

internal static class SqlErrorNumbers
{
    public const int UniqueIndexViolation = 2601;
    public const int UniqueConstraintViolation = 2627;
    public const int DatabaseReadOnly = 3906;
}
