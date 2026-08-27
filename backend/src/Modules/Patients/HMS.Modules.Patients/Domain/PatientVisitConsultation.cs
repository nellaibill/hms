namespace HMS.Modules.Patients.Domain;

/// <summary>
/// One consultant line on a <see cref="PatientVisit"/> — a patient can see several
/// consultants in the same visit ("Add another Consultant" on the frontend), so this is a
/// genuine 1:many child, not a flattened field. DepartmentId/ConsultantId/ConsultationTypeId
/// are app-level references into HMS.Modules.Masters' reference data (Department, Consultant,
/// ConsultationType) — validated by PatientVisitService before this is created, not enforced
/// by a database foreign key (cross-module references stay app-level-only per
/// docs/Architecture.md §4).
/// </summary>
internal class PatientVisitConsultation
{
    public Guid Id { get; private set; }
    public Guid VisitId { get; private set; }

    public Guid DepartmentId { get; private set; }
    public Guid ConsultantId { get; private set; }
    public Guid? ConsultationTypeId { get; private set; }

    // Required by EF Core materialization.
    private PatientVisitConsultation()
    {
    }

    private PatientVisitConsultation(Guid id, Guid visitId, Guid departmentId, Guid consultantId, Guid? consultationTypeId)
    {
        Id = id;
        VisitId = visitId;
        DepartmentId = departmentId;
        ConsultantId = consultantId;
        ConsultationTypeId = consultationTypeId;
    }

    public static PatientVisitConsultation Create(Guid visitId, Guid departmentId, Guid consultantId, Guid? consultationTypeId)
        => new(Guid.CreateVersion7(), visitId, departmentId, consultantId, consultationTypeId);
}
