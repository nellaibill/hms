namespace HMS.Modules.Patients.Contracts;

/// <summary>
/// Records one registration/encounter event for an existing patient — Encounter Type +
/// (for OP) Appointment Type, plus one or more consultation lines (primary consultant + any
/// "Add another Consultant" rows). All consultation lines created together share one VisitId;
/// a later, separate visit is a new POST with its own VisitId.
/// </summary>
public record CreatePatientVisitRequest
{
    public VisitType VisitType { get; init; }
    public Guid? AppointmentTypeId { get; init; }
    public IReadOnlyList<VisitConsultationRequest> Consultations { get; init; } = [];
}

public record VisitConsultationRequest
{
    public Guid DepartmentId { get; init; }
    public Guid ConsultantId { get; init; }
    public Guid? ConsultationTypeId { get; init; }
}

public record PatientVisitResponse
{
    public Guid VisitId { get; init; }
    public Guid PatientId { get; init; }
    public VisitType VisitType { get; init; }
    public Guid? AppointmentTypeId { get; init; }
    public IReadOnlyList<VisitConsultationResponse> Consultations { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

public record VisitConsultationResponse
{
    public Guid Id { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid ConsultantId { get; init; }
    public Guid? ConsultationTypeId { get; init; }
}
