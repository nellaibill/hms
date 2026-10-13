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

    /// <summary>The human-readable OP No ("OP-…", OP visits) or IP No ("IP-…", every other
    /// visit type), drawn from a sequence when the visit is recorded — see
    /// IPatientIdentifierGenerator.NextVisitNumberAsync. Null only on visits recorded before
    /// these numbers existed; they are not backfilled.</summary>
    public string? VisitNumber { get; private set; }

    public Guid? AppointmentTypeId { get; private set; }

    // Admission details — only ever set on a non-OP visit, see SetAdmissionDetails.
    public MedicoLegalType? AdmissionType { get; private set; }
    public string? ArNumber { get; private set; }
    public bool? IsReferred { get; private set; }
    public ReferralSource? ReferredBy { get; private set; }
    public VisitCategory? Category { get; private set; }

    private readonly List<PatientVisitConsultation> _consultations = [];
    public IReadOnlyCollection<PatientVisitConsultation> Consultations => _consultations.AsReadOnly();

    // Required by EF Core materialization.
    private PatientVisit()
    {
    }

    private PatientVisit(Guid id, Guid patientId, VisitType visitType, Guid? appointmentTypeId, Guid? createdBy, string? visitNumber)
        : base(id, createdBy)
    {
        PatientId = patientId;
        VisitType = visitType;
        AppointmentTypeId = appointmentTypeId;
        VisitNumber = visitNumber;
    }

    public static PatientVisit Create(Guid patientId, VisitType visitType, Guid? appointmentTypeId, Guid? createdBy, string? visitNumber = null)
        => new(Guid.CreateVersion7(), patientId, visitType, appointmentTypeId, createdBy, visitNumber);

    /// <summary>
    /// Records the admission details of a non-OP visit. An OP visit has none, so this is a
    /// no-op there. The two dependent answers are kept only while the answer they depend on
    /// calls for them — an A.R number belongs to an MLC, a referrer to a referred patient — so a
    /// stale value sent alongside a changed answer is dropped rather than stored.
    /// </summary>
    public void SetAdmissionDetails(MedicoLegalType? admissionType, string? arNumber, bool? isReferred, ReferralSource? referredBy, VisitCategory? category)
    {
        if (VisitType == VisitType.OP)
        {
            return;
        }

        AdmissionType = admissionType;
        ArNumber = admissionType == MedicoLegalType.MLC && !string.IsNullOrWhiteSpace(arNumber) ? arNumber.Trim() : null;
        IsReferred = isReferred;
        ReferredBy = isReferred == true ? referredBy : null;
        Category = category;
    }

    public void AddConsultation(PatientVisitConsultation consultation, Guid? updatedBy)
    {
        _consultations.Add(consultation);
        MarkUpdated(updatedBy);
    }
}
