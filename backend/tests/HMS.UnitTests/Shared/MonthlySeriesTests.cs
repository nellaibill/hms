using FluentAssertions;
using HMS.Shared.Kernel;
using Xunit;

namespace HMS.UnitTests.Shared;

// DASH-01: the Executive Dashboard's real-data charts bucket timestamps by month in hospital
// local time (IST, UTC+05:30).
public class MonthlySeriesTests
{
    private static readonly DateTime Now = new(2026, 9, 24, 6, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Build_ReturnsTheLastNMonthsOldestFirstWithZeroFill()
    {
        var series = MonthlySeries.Build([], months: 6, Now);

        series.Select(m => (m.Year, m.Month)).Should().Equal((2026, 4), (2026, 5), (2026, 6), (2026, 7), (2026, 8), (2026, 9));
        series.Should().OnlyContain(m => m.Value == 0);
    }

    [Fact]
    public void Build_BucketsByIndiaLocalTimeNotUtc()
    {
        // 31 Aug 20:00 UTC is already 1 Sep 01:30 IST — it belongs to September.
        var lateAugustUtc = new DateTime(2026, 8, 31, 20, 0, 0, DateTimeKind.Utc);
        var midAugustUtc = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);

        var series = MonthlySeries.Build([(lateAugustUtc, 1m), (midAugustUtc, 2m)], months: 2, Now);

        series.Should().Equal(new MonthlyTotal(2026, 8, 2m), new MonthlyTotal(2026, 9, 1m));
    }

    [Fact]
    public void Build_IgnoresPointsOutsideTheRange()
    {
        var series = MonthlySeries.Build([(new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc), 5m)], months: 3, Now);

        series.Sum(m => m.Value).Should().Be(0);
    }

    [Fact]
    public void StartUtc_IsMidnightIstOnTheFirstOfTheOldestMonth()
    {
        // 1 Apr 2026 00:00 IST == 31 Mar 2026 18:30 UTC.
        MonthlySeries.StartUtc(6, Now).Should().Be(new DateTime(2026, 3, 31, 18, 30, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(100, 24)]
    public void ClampMonths_KeepsTheRangeBetweenOneAndTwentyFour(int requested, int expected)
        => MonthlySeries.ClampMonths(requested).Should().Be(expected);
}
