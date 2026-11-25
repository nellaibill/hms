namespace HMS.Shared.Kernel;

/// <summary>
/// The hospital's time zone, India Standard Time, found the same way on every operating system.
/// Windows calls it "India Standard Time" and Linux calls it "Asia/Kolkata". .NET maps the
/// Windows name to "Asia/Calcutta", which newer Linux images (Ubuntu 24.04's tzdata) no longer
/// ship, so a plain FindSystemTimeZoneById("India Standard Time") threw there and took down
/// everything that touched it (the CI runner failed 143 tests on it). India has no daylight
/// saving, so if no zone file is found at all, a fixed UTC+05:30 zone is exactly right.
/// </summary>
public static class HospitalTimeZone
{
    private static readonly string[] Ids = ["India Standard Time", "Asia/Kolkata", "Asia/Calcutta"];

    public static TimeZoneInfo India { get; } = Resolve();

    /// <summary>The hospital's calendar day (India Standard Time) at a UTC instant. Every "today"
    /// on the server goes through here: the UTC day (<c>DateTime.UtcNow.Date</c>) still says
    /// yesterday until 5:30 AM in India (ADR-147).</summary>
    public static DateOnly DateAt(DateTime utc)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), India));

    /// <summary>The hospital's calendar day now, by the server's clock.</summary>
    public static DateOnly Today => DateAt(DateTime.UtcNow);

    /// <summary>The UTC instant a hospital calendar day starts at (00:00 IST = 18:30 UTC the day
    /// before), for comparing against stored UTC timestamps.</summary>
    public static DateTime DayStartUtc(DateOnly day)
        => TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), India);

    /// <summary>The UTC instant today (hospital day) started at.</summary>
    public static DateTime TodayStartUtc => DayStartUtc(Today);

    /// <summary>A date/time from a request (a list filter's From/To) as a UTC instant. One sent
    /// with its offset ("…Z", "+05:30") is that instant; one sent without (a bare
    /// "2026-11-25" or "2026-11-25T10:00") is the hospital's own wall-clock time, India Standard
    /// Time, never UTC or the server's zone.</summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => TimeZoneInfo.ConvertTimeToUtc(value, India),
    };

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in Ids)
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
            {
                return zone;
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("India Standard Time", TimeSpan.FromMinutes(330), "India Standard Time", "India Standard Time");
    }
}
