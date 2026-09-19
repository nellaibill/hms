using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// One row of the OPD Patient List — one per PatientVisitConsultation, denormalized with
/// Patient/Department/Consultant/AppointmentType display fields the same way
/// HMS.Modules.IPD.Contracts.AdmissionResponse denormalizes Patient/Ward/Bed, resolved once per
/// request in OpdQueryService, not stored.
/// </summary>
public record OpdPatientListItem
{
    public Guid ConsultationId { get; init; }
    public Guid VisitId { get; init; }
    public Guid PatientId { get; init; }
    public string Uhid { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public int Age { get; init; }
    public Gender Gender { get; init; }
    public DateTime AppointmentTime { get; init; }
    public Guid? AppointmentTypeId { get; init; }
    public string? AppointmentTypeName { get; init; }
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public Guid ConsultantId { get; init; }
    public string ConsultantName { get; init; } = string.Empty;
    public OpdConsultationStatus Status { get; init; }
}

public class OpdPatientListQuery : PagedRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ConsultantId { get; set; }
    public OpdConsultationStatus? Status { get; set; }
}

/// <summary>One row per consultant, for the OPD Consultation List tab — counts across every
/// consultation of theirs matching the query's date range/department, broken down by queue
/// status.</summary>
public record OpdConsultationSummaryItem
{
    public Guid ConsultantId { get; init; }
    public string ConsultantName { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public int TotalPatients { get; init; }
    public int Waiting { get; init; }
    public int InConsultation { get; init; }
    public int Completed { get; init; }

    /// <summary>Denormalized from Masters' Consultant.AvailableDays/VisitStartTime/VisitEndTime
    /// (see that entity's own doc comment) so the receptionist can see at a glance when this
    /// consultant is normally available, without a separate lookup. Empty/null when the
    /// consultant hasn't had this set yet.</summary>
    public IReadOnlyList<string> AvailableDays { get; init; } = [];
    public TimeOnly? VisitStartTime { get; init; }
    public TimeOnly? VisitEndTime { get; init; }
    public TimeOnly? VisitStartTime2 { get; init; }
    public TimeOnly? VisitEndTime2 { get; init; }
}

public record OpdConsultationSummaryQuery
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? ConsultantId { get; init; }
}
