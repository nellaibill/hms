namespace HMS.Modules.Appointments.Application;

/// <summary>What a consultant's day looks like for booking: which weekdays they hold clinic,
/// their one or two visiting sessions, and how those sessions are cut into slots.</summary>
internal sealed record ConsultantSchedule(
    IReadOnlySet<DayOfWeek> Days,
    IReadOnlyList<ScheduleSession> Sessions,
    int SlotMinutes,
    int PatientsPerSlot);

internal sealed record ScheduleSession(int Session, TimeOnly Start, TimeOnly End);

/// <summary>One slot of a planned day. <see cref="Index"/> is the slot's position in the whole
/// day (session 1 first), which is what token numbers are counted from.</summary>
internal sealed record PlannedSlot(int Session, TimeOnly Start, TimeOnly End, int Index)
{
    public int FirstToken(int patientsPerSlot) => (Index * patientsPerSlot) + 1;

    public int TokenFor(int seatNumber, int patientsPerSlot) => (Index * patientsPerSlot) + seatNumber;
}

/// <summary>
/// Cuts a consultant's visiting sessions into fixed-length slots. Pure and deterministic, so the
/// slot grid shown to reception and the check made when a booking is saved always agree.
///
/// A session that doesn't divide evenly drops its last, partial slot (09:00–10:10 with 15-minute
/// slots ends at 09:45–10:00). Tokens run 1, 2, 3… through the whole day in time order: with two
/// patients per slot, the 09:00 slot holds tokens 1 and 2, the 09:15 slot 3 and 4, and session 2
/// carries on after session 1's last token.
/// </summary>
internal static class SlotPlanner
{
    public static IReadOnlyList<PlannedSlot> PlanDay(ConsultantSchedule schedule, DateOnly date)
    {
        if (!schedule.Days.Contains(date.DayOfWeek) || schedule.SlotMinutes <= 0)
        {
            return [];
        }

        var slots = new List<PlannedSlot>();
        foreach (var session in schedule.Sessions.OrderBy(s => s.Start))
        {
            var startMinute = MinuteOfDay(session.Start);
            var endMinute = MinuteOfDay(session.End);

            for (var minute = startMinute; minute + schedule.SlotMinutes <= endMinute; minute += schedule.SlotMinutes)
            {
                slots.Add(new PlannedSlot(session.Session, FromMinute(minute), FromMinute(minute + schedule.SlotMinutes), slots.Count));
            }
        }

        return slots;
    }

    /// <summary>The first free seat (1-based) in a slot, or null when every seat is taken.</summary>
    public static int? FirstFreeSeat(IEnumerable<int> takenSeats, int patientsPerSlot)
    {
        var taken = takenSeats.ToHashSet();
        for (var seat = 1; seat <= patientsPerSlot; seat++)
        {
            if (!taken.Contains(seat))
            {
                return seat;
            }
        }

        return null;
    }

    private static int MinuteOfDay(TimeOnly time) => (time.Hour * 60) + time.Minute;

    // A session ending at 24:00 isn't representable as a TimeOnly, so the last slot of a session
    // that ends at 23:59 simply stops short — no slot ever wraps past midnight.
    private static TimeOnly FromMinute(int minute) => new(minute / 60, minute % 60);
}
