namespace JobScheduler.Dal.Contracts;

// Member names are persisted as strings and must equal the SQL CHECK literals exactly;
// EnumContractTests guards against drift.

public enum JobStatus
{
    Active,
    Inactive,
    Deleted,
}

public enum ConcurrencyMode
{
    Singleton,
    Concurrent,
}

public enum ScheduleType
{
    OneTime,
    Recurring,
}

public enum RepeatStepUnit
{
    Minute,
    Hour,
    Day,
    Week,
    Month,
}

public enum LaunchHttpMethod
{
    GET,
    POST,
    PUT,
    PATCH,
    DELETE,
}

public enum OutboxEventType
{
    JobCreated,
    JobUpdated,
    JobDeleted,
}

public enum JobRunStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    TimedOut,
    LockFailed,
    Cancelled,
}

public enum RunOutboxEventType
{
    ExecuteJob,
    NotifyFailure,
}
