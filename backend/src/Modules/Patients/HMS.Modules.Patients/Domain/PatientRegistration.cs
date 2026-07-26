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

    // Free-text placeholders until the Staff module exists to back these with a real
    // consultant/department master — see docs/BusinessRequirementsAnalysis.md's module
    // dependency notes and docs/DecisionLog.md's MVP-scope ADR.
    public string Department { get; private set; } = null!;
    public string Consultant { get; private set; } = null!;

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
        string department,
        string consultant,
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
        Department = department;
        Consultant = consultant;
        AdmissionType = admissionType;
        ReferralSource = referralSource;
        Category = category;
    }

    public static PatientRegistration Create(
        Guid patientId,
        string registrationNumber,
        EncounterType encounterType,
        ModeOfArrival modeOfArrival,
        string department,
        string consultant,
        AdmissionType? admissionType,
        string? referralSource,
        string? category,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(registrationNumber, nameof(registrationNumber));
        Guard.AgainstNullOrWhiteSpace(department, nameof(department));
        Guard.AgainstNullOrWhiteSpace(consultant, nameof(consultant));

        return new PatientRegistration(
            Guid.CreateVersion7(),
            patientId,
            registrationNumber.Trim(),
            encounterType,
            modeOfArrival,
            department.Trim(),
            consultant.Trim(),
            admissionType,
            referralSource?.Trim(),
            category?.Trim(),
            createdBy);
    }
}
