using HMS.Modules.Appointments.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Appointments.Domain;

/// <summary>
/// One booking in a consultant's slot — the aggregate root of this module. The patient is
/// either a registered one (<see cref="PatientId"/> set) or a first-time caller known only by
/// name and phone; both are kept as a snapshot so the day list never needs a cross-module call
/// to show who is coming. Department, consultant and appointment type are app-level references
/// into Masters (no database FK, per docs/Architecture.md §4).
///
/// Times are hospital-local: <see cref="AppointmentDate"/> plus <see cref="StartTime"/>/
/// <see cref="EndTime"/> are what reception sees on the wall clock, not UTC instants.
/// <see cref="SeatNumber"/> is which of the slot's seats this booking holds (1 when the
/// consultant sees one patient per slot); a unique index over consultant, date, start time and
/// seat for Booked/Arrived rows is what stops two desks taking the same seat at once.
/// </summary>
internal class Appointment : Entity
{
    public const int MaxNameLength = 150;
    public const int MaxNotesLength = 500;
    public const int MaxReasonLength = 300;

    public string AppointmentNumber { get; private set; } = null!;

    public Guid? PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public string? Uhid { get; private set; }

    public Guid DepartmentId { get; private set; }
    public Guid ConsultantId { get; private set; }
    public Guid? AppointmentTypeId { get; private set; }

    public DateOnly AppointmentDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int Session { get; private set; }
    public int SeatNumber { get; private set; }
    public int TokenNumber { get; private set; }

    public AppointmentStatus Status { get; private set; }
    public AppointmentSource Source { get; private set; }
    public string? Notes { get; private set; }

    public string? CancelReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledBy { get; private set; }

    public Guid? VisitId { get; private set; }
    public DateTime? ArrivedAt { get; private set; }
    public Guid? ArrivedBy { get; private set; }

    public DateTime? NoShowAt { get; private set; }

    public int RescheduleCount { get; private set; }

    // Required by EF Core materialization.
    private Appointment()
    {
    }

    private Appointment(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static Appointment Book(
        string appointmentNumber,
        AppointmentPatient patient,
        Guid departmentId,
        Guid consultantId,
        Guid? appointmentTypeId,
        SlotAssignment slot,
        AppointmentSource source,
        string? notes,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(appointmentNumber, nameof(appointmentNumber));
        Guard.AgainstNullOrWhiteSpace(patient.Name, nameof(patient.Name));
        Guard.AgainstNullOrWhiteSpace(patient.PhoneNumber, nameof(patient.PhoneNumber));

        var appointment = new Appointment(Guid.CreateVersion7(), createdBy)
        {
            AppointmentNumber = appointmentNumber,
            PatientId = patient.PatientId,
            PatientName = patient.Name.Trim(),
            PhoneNumber = patient.PhoneNumber.Trim(),
            Uhid = patient.Uhid,
            DepartmentId = departmentId,
            ConsultantId = consultantId,
            AppointmentTypeId = appointmentTypeId,
            Status = AppointmentStatus.Booked,
            Source = source,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };

        appointment.ApplySlot(slot);
        return appointment;
    }

    /// <summary>Booked and Arrived hold a seat; Cancelled and NoShow have given it back.</summary>
    public bool HoldsSeat => Status is AppointmentStatus.Booked or AppointmentStatus.Arrived;

    public void Reschedule(SlotAssignment slot, Guid? updatedBy)
    {
        EnsureBooked("reschedule");

        ApplySlot(slot);
        RescheduleCount++;
        MarkUpdated(updatedBy);
    }

    public void Cancel(string reason, Guid? cancelledBy)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));
        EnsureBooked("cancel");

        Status = AppointmentStatus.Cancelled;
        CancelReason = reason.Trim();
        CancelledAt = DateTime.UtcNow;
        CancelledBy = cancelledBy;
        MarkUpdated(cancelledBy);
    }

    public void MarkNoShow(Guid? updatedBy)
    {
        EnsureBooked("mark as no-show");

        Status = AppointmentStatus.NoShow;
        NoShowAt = DateTime.UtcNow;
        MarkUpdated(updatedBy);
    }

    /// <summary>Check-in. For a first-time caller this is also where the booking gets linked to
    /// the patient registered at the desk, so the snapshot is refreshed from that record.</summary>
    public void MarkArrived(AppointmentPatient patient, Guid visitId, Guid? arrivedBy)
    {
        EnsureBooked("check in");

        if (patient.PatientId is null)
        {
            throw new ArgumentException("A checked-in appointment must belong to a registered patient.", nameof(patient));
        }

        if (PatientId is not null && PatientId != patient.PatientId)
        {
            throw new InvalidOperationException("This appointment belongs to a different patient.");
        }

        PatientId = patient.PatientId;
        PatientName = patient.Name.Trim();
        PhoneNumber = patient.PhoneNumber.Trim();
        Uhid = patient.Uhid;
        VisitId = visitId;
        Status = AppointmentStatus.Arrived;
        ArrivedAt = DateTime.UtcNow;
        ArrivedBy = arrivedBy;
        MarkUpdated(arrivedBy);
    }

    private void ApplySlot(SlotAssignment slot)
    {
        if (slot.EndTime <= slot.StartTime)
        {
            throw new ArgumentException("A slot must end after it starts.", nameof(slot));
        }

        if (slot.Session is not (1 or 2))
        {
            throw new ArgumentException("Session must be 1 or 2.", nameof(slot));
        }

        if (slot.SeatNumber < 1 || slot.TokenNumber < 1)
        {
            throw new ArgumentException("Seat and token numbers start at 1.", nameof(slot));
        }

        AppointmentDate = slot.Date;
        StartTime = slot.StartTime;
        EndTime = slot.EndTime;
        Session = slot.Session;
        SeatNumber = slot.SeatNumber;
        TokenNumber = slot.TokenNumber;
    }

    private void EnsureBooked(string action)
    {
        if (Status != AppointmentStatus.Booked)
        {
            throw new InvalidOperationException($"Cannot {action} an appointment that is {Status}.");
        }
    }
}

/// <summary>Who the booking is for. <see cref="PatientId"/> and <see cref="Uhid"/> are null for a
/// first-time caller.</summary>
internal sealed record AppointmentPatient(Guid? PatientId, string Name, string PhoneNumber, string? Uhid);

/// <summary>A concrete seat in a concrete slot — what <see cref="Appointment"/> stores.</summary>
internal sealed record SlotAssignment(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, int Session, int SeatNumber, int TokenNumber);
