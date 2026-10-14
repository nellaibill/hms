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
}
