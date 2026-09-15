using HMS.Modules.OpdConsultation.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.OpdConsultation.Domain;

/// <summary>
/// One investigation line, always owned by exactly one OpdConsultationNote — a real, same-schema
/// DB foreign key. Name is free text, not a DiagnosticTest/DiagnosticService catalog reference —
/// per the approved plan, this is clinical documentation only ("the doctor wants a CBC and a
/// knee X-ray"), not an order that creates a real LabOrder; the existing OPD Billing -> Laboratory
/// flow (invoice-driven) is unchanged and unrelated to this list. The full list is replaced
/// wholesale on every save, same convention as diagnoses.
/// </summary>
internal class OpdConsultationInvestigation
{
    public Guid Id { get; private set; }
    public Guid OpdConsultationNoteId { get; private set; }
    public string Name { get; private set; } = null!;
    public OpdInvestigationDepartment Department { get; private set; }
    public OpdInvestigationPriority Priority { get; private set; }

    // Required by EF Core materialization.
    private OpdConsultationInvestigation()
    {
    }

    private OpdConsultationInvestigation(Guid id, Guid opdConsultationNoteId, string name, OpdInvestigationDepartment department, OpdInvestigationPriority priority)
    {
        Id = id;
        OpdConsultationNoteId = opdConsultationNoteId;
        Name = name;
        Department = department;
        Priority = priority;
    }

    public static OpdConsultationInvestigation Create(Guid opdConsultationNoteId, string name, OpdInvestigationDepartment department, OpdInvestigationPriority priority)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        return new(Guid.CreateVersion7(), opdConsultationNoteId, name.Trim(), department, priority);
    }
}
