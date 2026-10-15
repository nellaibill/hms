namespace HMS.Modules.Appointments.Contracts;

/// <summary>Lifecycle of one booking. Only <see cref="Booked"/> can change; the other three are
/// final. Stored as the member name (not its ordinal).</summary>
public enum AppointmentStatus
{
    /// <summary>Holds a seat in the consultant's slot.</summary>
    Booked,

    /// <summary>The patient came, and their OPD visit was recorded (VisitId is set).</summary>
    Arrived,

    Cancelled,

    /// <summary>The patient never came. Frees nothing extra — the day is already over or under way.</summary>
    NoShow,
}

/// <summary>How the booking reached the hospital.</summary>
public enum AppointmentSource
{
    Phone,
    InPerson,
    Other,
}
