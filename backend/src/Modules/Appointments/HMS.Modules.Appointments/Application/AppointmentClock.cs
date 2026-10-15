using HMS.Shared.Kernel;

namespace HMS.Modules.Appointments.Application;

/// <summary>
/// "Now" on the hospital's wall clock (India Standard Time, <see cref="MonthlySeries.HospitalTimeZone"/>)
/// — appointments are booked and compared in local dates and times. Wraps <see cref="TimeProvider"/>
/// so tests can pin the time.
/// </summary>
internal sealed class AppointmentClock
{
    private readonly TimeProvider _timeProvider;

    public AppointmentClock(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateTime NowLocal => TimeZoneInfo.ConvertTimeFromUtc(_timeProvider.GetUtcNow().UtcDateTime, MonthlySeries.HospitalTimeZone);

    public DateOnly Today => DateOnly.FromDateTime(NowLocal);

    public TimeOnly TimeOfDay => TimeOnly.FromDateTime(NowLocal);
}
