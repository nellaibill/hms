using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// One encounter/visit against a <see cref="Patient"/> — Form Section 7 of
/// docs/PatientRegistrationModule.md. A patient can have many registrations over time
/// (one per OP/IP/Emergency/Day-care visit); the UHID stays on <see cref="Patient"/>,
/// while <see cref="RegistrationNumber"/> is generated per-visit.
/// </summary>
internal class PatientRegistration : Entity
{
    public Guid PatientId { get; private set; }
    public string RegistrationNumber { get; private set; } = null!;
    public EncounterType EncounterType { get; private set; }
    public ModeOfArrival ModeOfArrival { get; private set; }

    // References into Masters' Department/Consultant reference data — validated against
    // IDepartmentService/IConsultantService in PatientService, not here (this module doesn't
    // depend on Masters' entities, only its public service seam).
    public Guid DepartmentId { get; private set; }
    public Guid ConsultantId { get; private set; }

    /// <summary>Optional OP appointment category (e.g. "New", "Follow-up") — a reference into
    /// Masters' AppointmentType, validated in PatientService the same way as DepartmentId/
    /// ConsultantId. Null for IP/Emergency/Day-care encounters, where it doesn't apply.</summary>
    public Guid? AppointmentTypeId { get; private set; }

    public AdmissionType? AdmissionType { get; private set; }
    public string? ReferralSource { get; private set; }

    // "Category" meaning is an open ambiguity per docs/BusinessRequirementsAnalysis.md
    // (patient category vs. payer category) — kept as a plain string until resolved.
    public string? Category { get; private set; }

    // Required by EF Core materialization.
    private PatientRegistration()
    {
    }

    private PatientRegistration(
        Guid id,
        Guid patientId,
        string registrationNumber,
        EncounterType encounterType,
        ModeOfArrival modeOfArrival,
        Guid departmentId,
        Guid consultantId,
        Guid? appointmentTypeId,
        AdmissionType? admissionType,
        string? referralSource,
        string? category,
        Guid? createdBy)
        : base(id, createdBy)
    {
        PatientId = patientId;
        RegistrationNumber = registrationNumber;
        EncounterType = encounterType;
        ModeOfArrival = modeOfArrival;
        DepartmentId = departmentId;
        ConsultantId = consultantId;
        AppointmentTypeId = appointmentTypeId;
        AdmissionType = admissionType;
        ReferralSource = referralSource;
        Category = category;
    }

    public static PatientRegistration Create(
        Guid patientId,
        string registrationNumber,
        EncounterType encounterType,
        ModeOfArrival modeOfArrival,
        Guid departmentId,
        Guid consultantId,
        Guid? appointmentTypeId,
        AdmissionType? admissionType,
        string? referralSource,
        string? category,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(registrationNumber, nameof(registrationNumber));

        return new PatientRegistration(
            Guid.CreateVersion7(),
            patientId,
            registrationNumber.Trim(),
            encounterType,
            modeOfArrival,
            departmentId,
            consultantId,
            appointmentTypeId,
            admissionType,
            referralSource?.Trim(),
            category?.Trim(),
            createdBy);
    }
}
