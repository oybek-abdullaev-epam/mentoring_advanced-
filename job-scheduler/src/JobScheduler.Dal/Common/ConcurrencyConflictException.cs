namespace JobScheduler.Dal.Common;

public sealed class ConcurrencyConflictException(Guid entityId, int expectedVersion)
    : Exception($"Entity {entityId} is not at expected version {expectedVersion} (stale version, or deleted).")
{
    public Guid EntityId { get; } = entityId;

    public int ExpectedVersion { get; } = expectedVersion;
}
