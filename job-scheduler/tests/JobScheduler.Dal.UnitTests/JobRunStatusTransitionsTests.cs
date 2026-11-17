using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobOrchestrator;

namespace JobScheduler.Dal.UnitTests;

public class JobRunStatusTransitionsTests
{
    private static readonly HashSet<(JobRunStatus From, JobRunStatus To)> Allowed =
    [
        (JobRunStatus.Pending, JobRunStatus.Running),
        (JobRunStatus.Pending, JobRunStatus.Cancelled),
        (JobRunStatus.Pending, JobRunStatus.TimedOut),
        (JobRunStatus.Running, JobRunStatus.Succeeded),
        (JobRunStatus.Running, JobRunStatus.Failed),
        (JobRunStatus.Running, JobRunStatus.TimedOut),
        (JobRunStatus.Running, JobRunStatus.Cancelled),
    ];

    public static TheoryData<JobRunStatus, JobRunStatus> AllPairs()
    {
        var data = new TheoryData<JobRunStatus, JobRunStatus>();
        foreach (var from in Enum.GetValues<JobRunStatus>())
        {
            foreach (var to in Enum.GetValues<JobRunStatus>())
            {
                data.Add(from, to);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void IsAllowed_MatchesTransitionTable(JobRunStatus from, JobRunStatus to)
    {
        Assert.Equal(Allowed.Contains((from, to)), JobRunStatusTransitions.IsAllowed(from, to));
    }

    [Theory]
    [InlineData(JobRunStatus.Pending, false)]
    [InlineData(JobRunStatus.Running, false)]
    [InlineData(JobRunStatus.Succeeded, true)]
    [InlineData(JobRunStatus.Failed, true)]
    [InlineData(JobRunStatus.TimedOut, true)]
    [InlineData(JobRunStatus.LockFailed, true)]
    [InlineData(JobRunStatus.Cancelled, true)]
    public void IsTerminal_ReturnsExpected(JobRunStatus status, bool expected)
    {
        Assert.Equal(expected, JobRunStatusTransitions.IsTerminal(status));
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void TerminalStatuses_HaveNoOutgoingTransitions(JobRunStatus from, JobRunStatus to)
    {
        if (JobRunStatusTransitions.IsTerminal(from))
        {
            Assert.False(JobRunStatusTransitions.IsAllowed(from, to));
        }
    }
}
