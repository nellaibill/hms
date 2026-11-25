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
}
