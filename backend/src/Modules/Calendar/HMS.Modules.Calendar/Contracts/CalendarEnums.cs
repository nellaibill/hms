namespace HMS.Modules.Calendar.Contracts;

/// <summary>
/// The fixed set of calendar event categories, per the Calendar Phase 1 spec.
/// "Hospital Event" and "Doctor Leave" become HospitalEvent/DoctorLeave — enum members
/// cannot contain spaces; the JSON wire format still serializes as the member's own name
/// via the global JsonStringEnumConverter (see HMS.Api/Program.cs), not the ordinal.
/// </summary>
public enum EventType
{
    Holiday,
    HospitalEvent,
    DoctorLeave,
    Meeting,
    Training,
    Maintenance,
    Other,
}
