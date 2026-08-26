using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// One registration/encounter event for a patient — the aggregate root for "Registration
/// Details". Consultations is a genuine 1:many child ("Add another Consultant" on the
/// frontend): when several consultants are added in the same registration, their rows all
/// share this visit's Id; a later, separate encounter is a brand new PatientVisit with its
/// own Id. Not a child of Patient's own aggregate (unlike Allergy/EmergencyContact) — it's
/// looked up/created independently via its own repository, just FK-bound to a patient.
/// </summary>
internal class PatientVisit : Entity
{
    public Guid PatientId { get; private set; }
    public VisitType VisitType { get; private set; }
    public Guid? AppointmentTypeId { get; private set; }

    private readonly List<PatientVisitConsultation> _consultations = [];
    public IReadOnlyCollection<PatientVisitConsultation> Consultations => _consultations.AsReadOnly();

    // Required by EF Core materialization.
    private PatientVisit()
    {
    }

    private PatientVisit(Guid id, Guid patientId, VisitType visitType, Guid? appointmentTypeId, Guid? createdBy)
        : base(id, createdBy)
    {
        PatientId = patientId;
        VisitType = visitType;
        AppointmentTypeId = appointmentTypeId;
    }

    public static PatientVisit Create(Guid patientId, VisitType visitType, Guid? appointmentTypeId, Guid? createdBy)
        => new(Guid.CreateVersion7(), patientId, visitType, appointmentTypeId, createdBy);

    public void AddConsultation(PatientVisitConsultation consultation, Guid? updatedBy)
    {
        _consultations.Add(consultation);
        MarkUpdated(updatedBy);
    }
}
