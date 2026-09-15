using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// One consultant line on a <see cref="PatientVisit"/> — a patient can see several
/// consultants in the same visit ("Add another Consultant" on the frontend), so this is a
/// genuine 1:many child, not a flattened field. DepartmentId/ConsultantId/ConsultationTypeId
/// are app-level references into HMS.Modules.Masters' reference data (Department, Consultant,
/// ConsultationType) — validated by PatientVisitService before this is created, not enforced
/// by a database foreign key (cross-module references stay app-level-only per
/// docs/Architecture.md §4). AppointmentTime/Status turn this line into the OPD queue's unit of
/// work: AppointmentTime defaults to "now" for today's walk-in-registration callers (Create's
/// appointmentTime parameter is optional), and Status walks the front-desk -> consultant-room ->
/// out lifecycle via the transition methods below — see Contracts/OpdContracts.cs for the read
/// side of this queue.
/// </summary>
internal class PatientVisitConsultation
{
    public Guid Id { get; private set; }
    public Guid VisitId { get; private set; }

    public Guid DepartmentId { get; private set; }
    public Guid ConsultantId { get; private set; }
    public Guid? ConsultationTypeId { get; private set; }

    public DateTime AppointmentTime { get; private set; }
    public OpdConsultationStatus Status { get; private set; }

    // Required by EF Core materialization.
    private PatientVisitConsultation()
    {
    }

    private PatientVisitConsultation(Guid id, Guid visitId, Guid departmentId, Guid consultantId, Guid? consultationTypeId, DateTime appointmentTime)
    {
        Id = id;
        VisitId = visitId;
        DepartmentId = departmentId;
        ConsultantId = consultantId;
        ConsultationTypeId = consultationTypeId;
        AppointmentTime = appointmentTime;
        Status = OpdConsultationStatus.Waiting;
    }

    public static PatientVisitConsultation Create(Guid visitId, Guid departmentId, Guid consultantId, Guid? consultationTypeId, DateTime? appointmentTime = null)
        => new(Guid.CreateVersion7(), visitId, departmentId, consultantId, consultationTypeId, appointmentTime ?? DateTime.UtcNow);

    /// <summary>Front desk marks the patient as physically present. Legal only from Waiting —
    /// once a consultation is underway or over, "checking in" no longer means anything.</summary>
    public void CheckIn()
    {
        if (Status != OpdConsultationStatus.Waiting)
        {
            throw new InvalidOperationException($"Cannot check in a consultation in status '{Status}'.");
        }

        Status = OpdConsultationStatus.CheckedIn;
    }

    /// <summary>The "Consult" action — a patient can be pulled straight from Waiting (front
    /// desk never checked them in) or from CheckedIn.</summary>
    public void StartConsultation()
    {
        if (Status != OpdConsultationStatus.Waiting && Status != OpdConsultationStatus.CheckedIn)
        {
            throw new InvalidOperationException($"Cannot start consultation for a consultation in status '{Status}'.");
        }

        Status = OpdConsultationStatus.InConsultation;
    }

    public void Complete()
    {
        if (Status != OpdConsultationStatus.InConsultation)
        {
            throw new InvalidOperationException($"Cannot complete a consultation in status '{Status}'.");
        }

        Status = OpdConsultationStatus.Completed;
    }

    /// <summary>The inverse of Complete — used when a completed consultation's clinical note is
    /// reopened for editing (HMS.Modules.OpdConsultation's ReopenAsync), so the queue's own
    /// status stays in sync with the note's Draft/Completed state the same way Complete already
    /// keeps them in sync going the other direction. Legal only from Completed.</summary>
    public void Reopen()
    {
        if (Status != OpdConsultationStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot reopen a consultation in status '{Status}'.");
        }

        Status = OpdConsultationStatus.InConsultation;
    }

    /// <summary>Legal from any non-terminal state — a patient can be pulled off the queue at
    /// any point before their consultation is actually finished.</summary>
    public void Cancel()
    {
        if (Status is OpdConsultationStatus.Completed or OpdConsultationStatus.Cancelled or OpdConsultationStatus.NoShow)
        {
            throw new InvalidOperationException($"Cannot cancel a consultation in status '{Status}'.");
        }

        Status = OpdConsultationStatus.Cancelled;
    }

    /// <summary>The patient never arrived — only meaningful before a consultation actually
    /// started.</summary>
    public void MarkNoShow()
    {
        if (Status != OpdConsultationStatus.Waiting && Status != OpdConsultationStatus.CheckedIn)
        {
            throw new InvalidOperationException($"Cannot mark a consultation in status '{Status}' as a no-show.");
        }

        Status = OpdConsultationStatus.NoShow;
    }
}
