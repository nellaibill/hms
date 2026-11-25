using FluentAssertions;
using HMS.Shared.Kernel;
using Xunit;

namespace HMS.UnitTests.Shared;

// The hospital's zone must resolve on Windows and on Linux images without the legacy
// Asia/Calcutta file (the CI runner), and always be UTC+05:30 with no daylight saving.
public class HospitalTimeZoneTests
{
    [Theory]
    [InlineData(2026, 1, 15)]
    [InlineData(2026, 7, 15)]
    public void India_IsFiveAndAHalfHoursAheadOfUtc_AllYearRound(int year, int month, int day)
    {
        var utc = new DateTime(year, month, day, 20, 0, 0, DateTimeKind.Utc);

        HospitalTimeZone.India.GetUtcOffset(utc).Should().Be(TimeSpan.FromMinutes(330));
    }

    [Fact]
    public void India_TurnsLateEveningUtcIntoTheNextHospitalDay()
    {
        var utc = new DateTime(2026, 11, 24, 19, 0, 0, DateTimeKind.Utc);

        TimeZoneInfo.ConvertTimeFromUtc(utc, HospitalTimeZone.India).Should().Be(new DateTime(2026, 11, 25, 0, 30, 0));
    }

    [Fact]
    public void MonthlySeries_UsesTheSameZone()
    {
        MonthlySeries.HospitalTimeZone.Should().BeSameAs(HospitalTimeZone.India);
    }

    // ADR-147: a bill or visit made at 12:30 AM IST belongs to that day, not the UTC day before.
    [Theory]
    [InlineData(2026, 11, 24, 18, 29, 2026, 11, 24)] // 11:59 PM IST on the 24th
    [InlineData(2026, 11, 24, 18, 30, 2026, 11, 25)] // 12:00 AM IST on the 25th
    [InlineData(2026, 11, 24, 23, 59, 2026, 11, 25)] // 5:29 AM IST on the 25th, UTC still says 24th
    [InlineData(2026, 11, 25, 0, 0, 2026, 11, 25)]
    public void DateAt_IsTheIndianCalendarDay(int y, int mo, int d, int h, int mi, int expectedY, int expectedMo, int expectedD)
    {
        HospitalTimeZone.DateAt(new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Utc))
            .Should().Be(new DateOnly(expectedY, expectedMo, expectedD));
    }

    [Fact]
    public void DayStartUtc_IsMidnightInIndia_SixPmThirtyUtcTheDayBefore()
    {
        var start = HospitalTimeZone.DayStartUtc(new DateOnly(2026, 11, 25));

        start.Should().Be(new DateTime(2026, 11, 24, 18, 30, 0, DateTimeKind.Utc));
        start.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Today_AndTodayStartUtc_AgreeWithTheServerClock()
    {
        var start = HospitalTimeZone.TodayStartUtc;

        HospitalTimeZone.DateAt(start).Should().Be(HospitalTimeZone.Today);
        DateTime.UtcNow.Should().BeOnOrAfter(start).And.BeBefore(start.AddDays(1));
    }

    [Fact]
    public void ToUtc_KeepsAnInstantSentWithItsOffset()
    {
        var instant = new DateTime(2026, 11, 24, 18, 30, 0, DateTimeKind.Utc);

        HospitalTimeZone.ToUtc(instant).Should().Be(instant);
    }

    [Fact]
    public void ToUtc_ReadsATimeWithoutAnOffsetAsIndianTime_NotUtc()
    {
        var bareDate = new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Unspecified);

        var utc = HospitalTimeZone.ToUtc(bareDate);

        utc.Should().Be(new DateTime(2026, 11, 24, 18, 30, 0, DateTimeKind.Utc));
        utc.Kind.Should().Be(DateTimeKind.Utc);
    }
}
