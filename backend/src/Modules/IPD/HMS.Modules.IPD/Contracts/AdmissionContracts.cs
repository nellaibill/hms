using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Contracts;

public record CreateAdmissionRequest
{
    public Guid PatientId { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid ConsultantId { get; init; }
    public Guid WardId { get; init; }
    public Guid BedId { get; init; }
    public DateTime? AdmissionDateTime { get; init; }
    public AdmissionType AdmissionType { get; init; }
    public string ReasonForAdmission { get; init; } = string.Empty;
}

// PatientId/WardId/BedId are intentionally absent — the patient can't be changed after
// admission, and ward/bed moves go through the dedicated transfer-bed workflow instead.
public record UpdateAdmissionRequest
{
    public Guid DepartmentId { get; init; }
    public Guid ConsultantId { get; init; }
    public AdmissionType AdmissionType { get; init; }
    public string ReasonForAdmission { get; init; } = string.Empty;
}

public record TransferBedRequest
{
    public Guid NewWardId { get; init; }
    public Guid NewBedId { get; init; }
    public string? TransferReason { get; init; }
}

public record DischargeAdmissionRequest
{
    public DateTime DischargeDateTime { get; init; }
    public DischargeType DischargeType { get; init; }
    public string? FinalDiagnosis { get; init; }
    public string? DischargeNotes { get; init; }
    public string? FollowUpAdvice { get; init; }
}

/// <summary>
/// Denormalizes a handful of Patients/Masters/Ward/Bed display fields (Uhid, PatientName,
/// Age, Gender, WardName, BedNumber, ConsultantName) so admission-list/dashboard consumers
/// don't need N extra round-trips per row — resolved once per request in AdmissionService,
/// not stored.
/// </summary>
public record AdmissionResponse
{
    public Guid Id { get; init; }
    public string AdmissionNumber { get; init; } = string.Empty;

    public Guid PatientId { get; init; }
    public string Uhid { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public int Age { get; init; }
    public string Gender { get; init; } = string.Empty;

    public Guid DepartmentId { get; init; }
    public Guid ConsultantId { get; init; }
    public string ConsultantName { get; init; } = string.Empty;
    public Guid WardId { get; init; }
    public string WardName { get; init; } = string.Empty;
    public Guid BedId { get; init; }
    public string BedNumber { get; init; } = string.Empty;

    public DateTime AdmissionDateTime { get; init; }
    public AdmissionType AdmissionType { get; init; }
    public string ReasonForAdmission { get; init; } = string.Empty;
    public AdmissionStatus Status { get; init; }

    public DateTime? DischargeDateTime { get; init; }
    public DischargeType? DischargeType { get; init; }
    public string? FinalDiagnosis { get; init; }
    public string? DischargeNotes { get; init; }
    public string? FollowUpAdvice { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class AdmissionListQuery : PagedRequest
{
    public AdmissionStatus? Status { get; set; }
    public Guid? WardId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ConsultantId { get; set; }
}
