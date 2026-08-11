using HMS.Modules.IPD.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// An inpatient stay — converts an existing Patients-module patient into an admitted
/// inpatient occupying a specific Ward/Bed. PatientId/DepartmentId/ConsultantId reference
/// Patients/Masters respectively; WardId/BedId reference this module's own Ward/Bed
/// aggregates. Bed occupancy side effects (Available/Occupied) are orchestrated by
/// AdmissionService, not by this entity, since they mutate a different aggregate (Bed).
/// </summary>
internal class Admission : Entity
{
    public string AdmissionNumber { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid ConsultantId { get; private set; }
    public Guid WardId { get; private set; }
    public Guid BedId { get; private set; }
    public DateTime AdmissionDateTime { get; private set; }
    public AdmissionType AdmissionType { get; private set; }
    public string ReasonForAdmission { get; private set; } = null!;
    public AdmissionStatus Status { get; private set; }

    public DateTime? DischargeDateTime { get; private set; }
    public DischargeType? DischargeType { get; private set; }
    public string? FinalDiagnosis { get; private set; }
    public string? DischargeNotes { get; private set; }
    public string? FollowUpAdvice { get; private set; }

    // Required by EF Core materialization.
    private Admission()
    {
    }

    private Admission(
        Guid id,
        string admissionNumber,
        Guid patientId,
        Guid departmentId,
        Guid consultantId,
        Guid wardId,
        Guid bedId,
        DateTime admissionDateTime,
        AdmissionType admissionType,
        string reasonForAdmission,
        Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionNumber = admissionNumber;
        PatientId = patientId;
        DepartmentId = departmentId;
        ConsultantId = consultantId;
        WardId = wardId;
        BedId = bedId;
        AdmissionDateTime = admissionDateTime;
        AdmissionType = admissionType;
        ReasonForAdmission = reasonForAdmission;
        Status = AdmissionStatus.Admitted;
    }

    public static Admission Create(
        string admissionNumber,
        Guid patientId,
        Guid departmentId,
        Guid consultantId,
        Guid wardId,
        Guid bedId,
        DateTime admissionDateTime,
        AdmissionType admissionType,
        string reasonForAdmission,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(admissionNumber, nameof(admissionNumber));
        Guard.AgainstNullOrWhiteSpace(reasonForAdmission, nameof(reasonForAdmission));

        return new Admission(
            Guid.CreateVersion7(),
            admissionNumber,
            patientId,
            departmentId,
            consultantId,
            wardId,
            bedId,
            admissionDateTime,
            admissionType,
            reasonForAdmission.Trim(),
            createdBy);
    }

    public void Update(
        Guid departmentId,
        Guid consultantId,
        AdmissionType admissionType,
        string reasonForAdmission,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(reasonForAdmission, nameof(reasonForAdmission));

        DepartmentId = departmentId;
        ConsultantId = consultantId;
        AdmissionType = admissionType;
        ReasonForAdmission = reasonForAdmission.Trim();
        MarkUpdated(updatedBy);
    }

    /// <summary>Caller (AdmissionService) is responsible for flipping the old/new beds'
    /// Available/Occupied status and for rejecting the call while not Admitted.</summary>
    public void TransferBed(Guid newWardId, Guid newBedId, Guid? updatedBy)
    {
        WardId = newWardId;
        BedId = newBedId;
        MarkUpdated(updatedBy);
    }

    /// <summary>Caller (AdmissionService) is responsible for validating the discharge date
    /// against AdmissionDateTime and for freeing the bed.</summary>
    public void Discharge(
        DateTime dischargeDateTime,
        Contracts.DischargeType dischargeType,
        string? finalDiagnosis,
        string? dischargeNotes,
        string? followUpAdvice,
        Guid? updatedBy)
    {
        Status = AdmissionStatus.Discharged;
        DischargeDateTime = dischargeDateTime;
        DischargeType = dischargeType;
        FinalDiagnosis = finalDiagnosis;
        DischargeNotes = dischargeNotes;
        FollowUpAdvice = followUpAdvice;
        MarkUpdated(updatedBy);
    }
}
