using System.Diagnostics;
using System.Globalization;
using JobScheduler.Dal.Contracts;
using JobScheduler.Dal.JobOrchestrator;

namespace JobScheduler.Dal.UnitTests;

public class NextRunCalculatorTests
{
    private static DateTime D(string value) => DateTime.Parse(value, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("2026-11-13T10:00:00", 1, RepeatStepUnit.Minute, "2026-11-13T10:00:30", "2026-11-13T10:01:00")]
    [InlineData("2026-11-13T10:00:00", 5, RepeatStepUnit.Minute, "2026-11-13T10:15:00", "2026-11-13T10:20:00")]
    [InlineData("2026-01-01T00:00:00", 2, RepeatStepUnit.Hour, "2026-01-01T05:00:00", "2026-01-01T06:00:00")]
    [InlineData("2026-01-01T02:00:00", 1, RepeatStepUnit.Day, "2026-01-05T01:00:00", "2026-01-05T02:00:00")]
    [InlineData("2026-01-01T00:00:00", 1, RepeatStepUnit.Week, "2026-01-09T00:00:00", "2026-01-15T00:00:00")]
    [InlineData("2026-01-31T00:00:00", 1, RepeatStepUnit.Month, "2026-02-01T00:00:00", "2026-02-28T00:00:00")]
    [InlineData("2026-01-15T00:00:00", 3, RepeatStepUnit.Month, "2026-05-01T00:00:00", "2026-07-15T00:00:00")]
    [InlineData("2024-02-29T00:00:00", 12, RepeatStepUnit.Month, "2025-03-01T00:00:00", "2026-02-28T00:00:00")]
    public void Next_ReturnsFirstOccurrenceAfterNow(string from, int step, RepeatStepUnit unit, string now, string expected)
    {
        Assert.Equal(D(expected), NextRunCalculator.Next(D(from), step, unit, D(now)));
    }

    [Theory]
    [InlineData("2026-11-13T10:00:00", 1, RepeatStepUnit.Minute, "2026-11-13T10:03:00", "2026-11-13T10:04:00")]
    [InlineData("2026-01-01T00:00:00", 1, RepeatStepUnit.Day, "2026-01-03T00:00:00", "2026-01-04T00:00:00")]
    [InlineData("2026-01-31T00:00:00", 1, RepeatStepUnit.Month, "2026-02-28T00:00:00", "2026-03-31T00:00:00")]
    public void Next_WhenNowIsExactlyAnOccurrence_ReturnsTheFollowingOne(string from, int step, RepeatStepUnit unit, string now, string expected)
    {
        Assert.Equal(D(expected), NextRunCalculator.Next(D(from), step, unit, D(now)));
    }

    [Theory]
    [InlineData(RepeatStepUnit.Minute, "2030-01-01T00:01:00")]
    [InlineData(RepeatStepUnit.Hour, "2030-01-01T01:00:00")]
    [InlineData(RepeatStepUnit.Day, "2030-01-02T00:00:00")]
    [InlineData(RepeatStepUnit.Week, "2030-01-08T00:00:00")]
    [InlineData(RepeatStepUnit.Month, "2030-02-01T00:00:00")]
    public void Next_WhenFromIsInTheFuture_ReturnsFromPlusOneStep(RepeatStepUnit unit, string expected)
    {
        Assert.Equal(D(expected), NextRunCalculator.Next(D("2030-01-01T00:00:00"), 1, unit, D("2026-11-13T10:00:00")));
    }

    [Fact]
    public void Next_MonthEnd_DoesNotDriftAfterShortMonth()
    {
        var from = D("2026-01-31T00:00:00");

        Assert.Equal(D("2026-03-31T00:00:00"), NextRunCalculator.Next(from, 1, RepeatStepUnit.Month, D("2026-03-01T00:00:00")));
    }

    [Fact]
    public void Next_FromYearsInThePast_IsCorrectAndFast()
    {
        var from = D("2001-01-01T00:00:00");
        var now = D("2026-11-13T10:00:30");
        NextRunCalculator.Next(from, 1, RepeatStepUnit.Minute, now); // warm-up so JIT time is not measured

        var stopwatch = Stopwatch.StartNew();
        var next = NextRunCalculator.Next(from, 1, RepeatStepUnit.Minute, now);
        stopwatch.Stop();

        Assert.Equal(D("2026-11-13T10:01:00"), next);
        Assert.True(stopwatch.ElapsedMilliseconds < 50, $"Took {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void Next_KeepsKindOfFrom()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(DateTimeKind.Utc, NextRunCalculator.Next(from, 1, RepeatStepUnit.Hour, from.AddHours(3.5)).Kind);
        Assert.Equal(DateTimeKind.Utc, NextRunCalculator.Next(from, 1, RepeatStepUnit.Month, from.AddDays(40)).Kind);
    }

    [Theory]
    [InlineData(RepeatStepUnit.Minute, 0)]
    [InlineData(RepeatStepUnit.Month, 0)]
    [InlineData(RepeatStepUnit.Day, -1)]
    [InlineData(RepeatStepUnit.Month, -3)]
    public void Next_NonPositiveStep_Throws(RepeatStepUnit unit, int step)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NextRunCalculator.Next(D("2026-01-01T00:00:00"), step, unit, D("2026-02-01T00:00:00")));
    }

    [Theory]
    [InlineData(RepeatStepUnit.Week)]
    [InlineData(RepeatStepUnit.Month)]
    public void Next_ResultBeyondMaxDate_Throws(RepeatStepUnit unit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NextRunCalculator.Next(D("2026-01-01T00:00:00"), int.MaxValue, unit, D("2026-02-01T00:00:00")));
    }

    [Fact]
    public void Next_AgreesWithNaiveSearch()
    {
        var random = new Random(42);
        var maxOffsetDays = new Dictionary<RepeatStepUnit, int>
        {
            [RepeatStepUnit.Minute] = 2,
            [RepeatStepUnit.Hour] = 60,
            [RepeatStepUnit.Day] = 800,
            [RepeatStepUnit.Week] = 800,
            [RepeatStepUnit.Month] = 1500,
        };

        for (var i = 0; i < 3000; i++)
        {
            var unit = (RepeatStepUnit)random.Next(5);
            var step = random.Next(1, unit == RepeatStepUnit.Month ? 15 : 8);
            var from = new DateTime(2024, 1, 1).AddDays(random.Next(0, 800)).AddMinutes(random.Next(0, 24 * 60));
            var now = from.AddMinutes(random.Next(-10 * 24 * 60, maxOffsetDays[unit] * 24 * 60));

            Assert.Equal(NaiveNext(from, step, unit, now), NextRunCalculator.Next(from, step, unit, now));
        }
    }

    private static DateTime NaiveNext(DateTime from, int step, RepeatStepUnit unit, DateTime now)
    {
        for (var k = 1; ; k++)
        {
            var candidate = unit switch
            {
                RepeatStepUnit.Minute => from.AddMinutes((double)k * step),
                RepeatStepUnit.Hour => from.AddHours((double)k * step),
                RepeatStepUnit.Day => from.AddDays((double)k * step),
                RepeatStepUnit.Week => from.AddDays(7.0 * k * step),
                _ => from.AddMonths(k * step),
            };

            if (candidate > now)
            {
                return candidate;
            }
        }
    }
}
