using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

internal static class PharmacyClock
{
    /// <summary>A batch within this many days of its expiry date is "expiring soon" — the
    /// same window the stock balance table has always flagged.</summary>
    public const int ExpiringSoonWindowDays = 30;

    /// <summary>
    /// Today's date at the hospital, not in UTC. Expiry is a calendar-date rule, and the UTC
    /// date trails India's by 5½ hours — so judged in UTC, a batch that expired yesterday
    /// would stay dispensable until 05:30 every morning.
    /// </summary>
    public static DateOnly Today()
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MonthlySeries.HospitalTimeZone));

    /// <summary>The UTC instant a hospital-local calendar day begins — for turning a "from
    /// date / to date" filter into a range over timestamps stored in UTC.</summary>
    public static DateTime StartOfDayUtc(DateOnly date)
        => TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), MonthlySeries.HospitalTimeZone);

    /// <summary>The hospital-local calendar day a UTC instant falls on.</summary>
    public static DateOnly LocalDate(DateTime utc)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), MonthlySeries.HospitalTimeZone));
}
