using HMS.Shared.Kernel;

namespace HMS.Modules.Appointments.Contracts;

/// <summary>
/// Books one seat in a consultant's slot. Either <see cref="PatientId"/> (a registered patient —
/// name, phone and UHID are copied from their record) or <see cref="PatientName"/> plus
/// <see cref="PhoneNumber"/> (a first-time caller, registered when they arrive). Dates and times
/// are hospital-local (India Standard Time); <see cref="StartTime"/> must be the start of one of
/// the consultant's slots for that day (see GET /appointments/slots).
/// </summary>
public record BookAppointmentRequest
{
    public Guid? PatientId { get; init; }
    public string? PatientName { get; init; }
    public string? PhoneNumber { get; init; }

    public Guid DepartmentId { get; init; }
    public Guid ConsultantId { get; init; }
    public Guid? AppointmentTypeId { get; init; }

    public DateOnly AppointmentDate { get; init; }
    public TimeOnly StartTime { get; init; }

    public AppointmentSource Source { get; init; } = AppointmentSource.Phone;
    public string? Notes { get; init; }
}

/// <summary>Moves a Booked appointment to another slot of the same consultant.</summary>
public record RescheduleAppointmentRequest
{
    public DateOnly AppointmentDate { get; init; }
    public TimeOnly StartTime { get; init; }
}

public record CancelAppointmentRequest
{
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Check-in: the visit reception just recorded for this patient through the Patients
/// module. For a first-time caller, <see cref="PatientId"/> is the patient registered at the desk.</summary>
public record MarkAppointmentArrivedRequest
{
    public Guid PatientId { get; init; }
    public Guid VisitId { get; init; }
}

public record AppointmentResponse
{
    public Guid Id { get; init; }
    public string AppointmentNumber { get; init; } = string.Empty;

    /// <summary>Null while the booking is for a first-time caller who isn't registered yet.</summary>
    public Guid? PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Uhid { get; init; }
    public bool IsNewPatient { get; init; }

    public Guid DepartmentId { get; init; }
    public Guid ConsultantId { get; init; }
    public Guid? AppointmentTypeId { get; init; }

    public DateOnly AppointmentDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }

    /// <summary>1 = the consultant's first visiting session, 2 = the second.</summary>
    public int Session { get; init; }
    public int TokenNumber { get; init; }

    public AppointmentStatus Status { get; init; }
    public AppointmentSource Source { get; init; }
    public string? Notes { get; init; }

    public string? CancelReason { get; init; }
    public DateTime? CancelledAt { get; init; }
    public Guid? VisitId { get; init; }
    public DateTime? ArrivedAt { get; init; }
    public DateTime? NoShowAt { get; init; }
    public int RescheduleCount { get; init; }

    public DateTime CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class AppointmentListQuery : PagedRequest
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? ConsultantId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PatientId { get; set; }
    public AppointmentStatus? Status { get; set; }
}

public class AppointmentSlotsQuery
{
    public Guid ConsultantId { get; set; }
    public DateOnly Date { get; set; }
}

/// <summary>One consultant's bookable day. When <see cref="IsWorkingDay"/> is false,
/// <see cref="Message"/> says why (not a clinic day, no visiting hours set, on leave).</summary>
public record AppointmentSlotsResponse
{
    public Guid ConsultantId { get; init; }
    public DateOnly Date { get; init; }
    public bool IsWorkingDay { get; init; }
    public string? Message { get; init; }
    public int SlotMinutes { get; init; }
    public int PatientsPerSlot { get; init; }
    public IReadOnlyList<AppointmentSlotResponse> Slots { get; init; } = [];
}

public record AppointmentSlotResponse
{
    public int Session { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }

    /// <summary>The token the slot's first seat gets; seat N gets FirstToken + N - 1.</summary>
    public int FirstToken { get; init; }
    public int Capacity { get; init; }
    public int Booked { get; init; }
    public int Available { get; init; }
    public bool IsBlocked { get; init; }
    public string? BlockReason { get; init; }
    public bool IsPast { get; init; }
    public bool IsBookable { get; init; }
}

public class AppointmentSummaryQuery
{
    public DateOnly Date { get; set; }
    public Guid? ConsultantId { get; set; }
    public Guid? DepartmentId { get; set; }
}

public record AppointmentSummaryResponse
{
    public DateOnly Date { get; init; }
    public int Total { get; init; }
    public int Booked { get; init; }
    public int Arrived { get; init; }
    public int Cancelled { get; init; }
    public int NoShow { get; init; }
}

/// <summary>Closes a consultant's slots — a whole day, or just one session (1 or 2) — for every
/// date from <see cref="FromDate"/> to <see cref="ToDate"/> inclusive.</summary>
public record CreateConsultantBlockRequest
{
    public Guid ConsultantId { get; init; }
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }

    /// <summary>Null = the whole day.</summary>
    public int? Session { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record ConsultantBlockResponse
{
    public Guid Id { get; init; }
    public Guid ConsultantId { get; init; }
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public int? Session { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
}

/// <summary>The new block, plus the Booked appointments that now fall inside it. They are not
/// cancelled automatically — reception calls the patients and reschedules or cancels them.</summary>
public record ConsultantBlockCreatedResponse
{
    public ConsultantBlockResponse Block { get; init; } = new();
    public IReadOnlyList<AppointmentResponse> AffectedAppointments { get; init; } = [];
}

public class ConsultantBlockListQuery
{
    public Guid? ConsultantId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
