using JobScheduler.Dal.Contracts;

namespace JobScheduler.Dal.JobOrchestrator;

public static class NextRunCalculator
{
    // Returns the first from + k*step (k >= 1) strictly after utcNow. Missed windows are skipped, not back-filled:
    // after downtime a job fires once and then resumes on its original cadence.
    public static DateTime Next(DateTime from, int stepSize, RepeatStepUnit unit, DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepSize);

        return unit == RepeatStepUnit.Month
            ? NextMonthly(from, stepSize, utcNow)
            : NextFixed(from, stepSize, unit, utcNow);
    }

    // Closed form instead of a loop: a job created in 2001 with a 1-minute step is ~13M steps behind.
    // Int128 because stepSize * ticks-per-week overflows long for large step sizes.
    private static DateTime NextFixed(DateTime from, int stepSize, RepeatStepUnit unit, DateTime utcNow)
    {
        Int128 stepTicks = (Int128)stepSize * TicksPerUnit(unit);
        long elapsedTicks = utcNow.Ticks - from.Ticks;
        Int128 steps = elapsedTicks < 0 ? 1 : elapsedTicks / stepTicks + 1;

        Int128 resultTicks = from.Ticks + steps * stepTicks;
        if (resultTicks > DateTime.MaxValue.Ticks)
        {
            throw new ArgumentOutOfRangeException(nameof(stepSize), stepSize, "Next run is beyond DateTime.MaxValue.");
        }

        return new DateTime((long)resultTicks, from.Kind);
    }

    // Every candidate is from.AddMonths(k*step), never the previous candidate plus a step: day-of-month clamping
    // is lossy (Jan 31 -> Feb 28 -> Mar 28 would drift), while from-based gives Jan 31 -> Feb 28 -> Mar 31.
    // With q = whole steps between the two calendar months, candidate q lands in or before now's month and
    // candidate q+1 is always in a later month, so the answer is q or q+1 and the loop is bounded.
    private static DateTime NextMonthly(DateTime from, int stepSize, DateTime utcNow)
    {
        int monthsElapsed = (utcNow.Year - from.Year) * 12 + utcNow.Month - from.Month;
        long steps = Math.Max(1, monthsElapsed / stepSize);

        for (var attempt = 0; attempt < 2; attempt++, steps++)
        {
            long months = steps * stepSize;
            if (months > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(stepSize), stepSize, "Next run is beyond DateTime.MaxValue.");
            }

            var candidate = from.AddMonths((int)months);
            if (candidate > utcNow)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Month step search exceeded its bound; this indicates a bug in NextRunCalculator.");
    }

    private static long TicksPerUnit(RepeatStepUnit unit) => unit switch
    {
        RepeatStepUnit.Minute => TimeSpan.TicksPerMinute,
        RepeatStepUnit.Hour => TimeSpan.TicksPerHour,
        RepeatStepUnit.Day => TimeSpan.TicksPerDay,
        RepeatStepUnit.Week => 7 * TimeSpan.TicksPerDay,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
    };
}
