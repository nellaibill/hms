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
    /// visit type), drawn from a sequence — see IPatientIdentifierGenerator.NextVisitNumberAsync.
    /// An IP No is drawn when the visit is recorded; an OP No only once the visit's consultation
    /// fee is paid (ADR-103, see <see cref="AssignVisitNumber"/>), so it is null on an unpaid OP
    /// visit. Also null on visits recorded before these numbers existed; they are not backfilled.</summary>
    public string? VisitNumber { get; private set; }

    public Guid? AppointmentTypeId { get; private set; }

    /// <summary>ICU / Casualty / LR — only ever set on an Emergency visit, see SetEmergencyType.</summary>
    public EmergencyType? EmergencyType { get; private set; }

    // Admission details — only ever set on a non-OP visit, see SetAdmissionDetails.
    public MedicoLegalType? AdmissionType { get; private set; }
    public string? ArNumber { get; private set; }
    public bool? IsReferred { get; private set; }
    public ReferralSource? ReferredBy { get; private set; }

    /// <summary>The referrer's name and mobile number ("Referral name" / "Referral mobile") — kept only on a
    /// referred visit, like <see cref="ReferredBy"/>.</summary>
    public string? ReferralName { get; private set; }
    public string? ReferralMobile { get; private set; }

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
    public void SetAdmissionDetails(
        MedicoLegalType? admissionType,
        string? arNumber,
        bool? isReferred,
        ReferralSource? referredBy,
        string? referralName,
        string? referralMobile,
        VisitCategory? category)
    {
        if (VisitType == VisitType.OP)
        {
            return;
        }

        AdmissionType = admissionType;
        ArNumber = admissionType == MedicoLegalType.MLC && !string.IsNullOrWhiteSpace(arNumber) ? arNumber.Trim() : null;
        IsReferred = isReferred;
        ReferredBy = isReferred == true ? referredBy : null;
        ReferralName = isReferred == true && !string.IsNullOrWhiteSpace(referralName) ? referralName.Trim() : null;
        ReferralMobile = isReferred == true && !string.IsNullOrWhiteSpace(referralMobile) ? referralMobile.Trim() : null;
        Category = category;
    }

    /// <summary>Records where an Emergency patient is being seen. Any other visit type has no
    /// emergency type, so a value sent with one is dropped rather than stored.</summary>
    public void SetEmergencyType(EmergencyType? emergencyType)
    {
        EmergencyType = VisitType == VisitType.Emergency ? emergencyType : null;
    }

    /// <summary>Gives a visit recorded without a number its OP No — an OP visit once its
    /// consultation fee is paid (ADR-103). A visit that already has a number keeps it: returns
    /// false and changes nothing, so a second payment can never renumber a visit.</summary>
    public bool AssignVisitNumber(string visitNumber, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(visitNumber, nameof(visitNumber));

        if (VisitNumber is not null)
        {
            return false;
        }

        VisitNumber = visitNumber.Trim();
        MarkUpdated(updatedBy);
        return true;
    }

    public void AddConsultation(PatientVisitConsultation consultation, Guid? updatedBy)
    {
        _consultations.Add(consultation);
        MarkUpdated(updatedBy);
    }
}
