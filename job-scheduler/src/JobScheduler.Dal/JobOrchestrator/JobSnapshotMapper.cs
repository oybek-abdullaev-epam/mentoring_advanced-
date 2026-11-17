using System.Text.Json;
using JobScheduler.Dal.Common;
using JobScheduler.Dal.Contracts;

namespace JobScheduler.Dal.JobOrchestrator;

public static class JobSnapshotMapper
{
    public static JobScheduleSnapshot ToSnapshot(JobSnapshotPayload p, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentNullException.ThrowIfNull(p.LaunchDetails);
        DateTimeGuard.ThrowIfLocal(utcNow, nameof(utcNow));
        DateTimeGuard.ThrowIfLocal(p.StartAt, nameof(p.StartAt));

        return new JobScheduleSnapshot
        {
            JobId = p.JobId,
            UserId = p.UserId,
            ConcurrencyMode = p.ConcurrencyMode,
            ScheduleType = p.ScheduleType,
            RepeatStepSize = p.RepeatStepSize,
            RepeatStepUnit = p.RepeatStepUnit,
            NextRunAt = ComputeNextRunAt(p, utcNow),
            LaunchDetails = JsonSerializer.Serialize(p.LaunchDetails, Json.Default),
            Status = p.Status,
            Version = p.Version,
            CreatedAt = utcNow,
        };
    }

    // A OneTime job keeps its StartAt even when it is already overdue, so the claimer still fires it once.
    // A recurring job whose StartAt is in the past (or exactly now) goes through Next, which is strictly
    // after utcNow: the occurrence at StartAt itself is treated as missed rather than fired late.
    private static DateTime ComputeNextRunAt(JobSnapshotPayload p, DateTime utcNow)
    {
        if (p.ScheduleType == ScheduleType.Recurring && (p.RepeatStepSize is not > 0 || p.RepeatStepUnit is null))
        {
            throw new ArgumentException("A recurring schedule requires a positive RepeatStepSize and a RepeatStepUnit.", nameof(p));
        }

        if (p.ScheduleType == ScheduleType.OneTime || p.StartAt > utcNow)
        {
            return p.StartAt;
        }

        return NextRunCalculator.Next(p.StartAt, p.RepeatStepSize!.Value, p.RepeatStepUnit!.Value, utcNow);
    }
}
