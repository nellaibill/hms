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
