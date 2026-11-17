namespace JobScheduler.Dal.Common;

public sealed class DuplicateJobNameException(Guid userId, string name, Exception? inner = null)
    : Exception($"User {userId} already has a job named '{name}'.", inner)
{
    public Guid UserId { get; } = userId;

    public string Name { get; } = name;
}
