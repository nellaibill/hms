namespace HMS.Shared.Kernel;

/// <summary>One calendar month's total in a dashboard series — a count (visits, admissions) or
/// an amount (revenue), depending on the endpoint.</summary>
public record MonthlyTotal(int Year, int Month, decimal Value);

/// <summary>
/// Buckets timestamped values into the last N calendar months in the hospital's local time
/// (India Standard Time — the same zone the daily backup schedule uses), oldest first, with a
/// zero entry for any month that had nothing so a chart never skips a month. Built for the
/// Executive Dashboard's real-data charts (regression report DASH-01), which used to be
/// hard-coded mock numbers.
/// </summary>
public static class MonthlySeries
{
    public const int MaxMonths = 24;

    public static readonly TimeZoneInfo HospitalTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    public static int ClampMonths(int months) => Math.Clamp(months, 1, MaxMonths);

    /// <summary>The UTC instant the series starts at: 00:00 local time on the 1st of the oldest
    /// month in range. Callers query their data from here.</summary>
    public static DateTime StartUtc(int months, DateTime utcNow)
    {
        months = ClampMonths(months);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), HospitalTimeZone);
        var firstOfOldestMonth = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(-(months - 1));
        return TimeZoneInfo.ConvertTimeToUtc(firstOfOldestMonth, HospitalTimeZone);
    }

    public static IReadOnlyList<MonthlyTotal> Build(IEnumerable<(DateTime AtUtc, decimal Value)> points, int months, DateTime utcNow)
    {
        months = ClampMonths(months);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), HospitalTimeZone);
        var current = new DateTime(localNow.Year, localNow.Month, 1);

        var totals = new Dictionary<(int Year, int Month), decimal>();
        for (var i = months - 1; i >= 0; i--)
        {
            var month = current.AddMonths(-i);
            totals[(month.Year, month.Month)] = 0m;
        }

        foreach (var (atUtc, value) in points)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(atUtc, DateTimeKind.Utc), HospitalTimeZone);
            var key = (local.Year, local.Month);
            if (totals.ContainsKey(key))
            {
                totals[key] += value;
            }
        }

        return totals
            .OrderBy(t => t.Key.Year)
            .ThenBy(t => t.Key.Month)
            .Select(t => new MonthlyTotal(t.Key.Year, t.Key.Month, t.Value))
            .ToList();
    }
}
