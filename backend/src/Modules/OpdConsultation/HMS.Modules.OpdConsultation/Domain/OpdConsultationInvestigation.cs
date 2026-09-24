using HMS.Modules.OpdConsultation.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.OpdConsultation.Domain;

/// <summary>
/// One investigation line, always owned by exactly one OpdConsultationNote — a real, same-schema
/// DB foreign key. Name is always stored (free text, or the picked catalog service's name).
/// ServiceId optionally links the line to Masters' DiagnosticService catalog: when set, OPD
/// Billing Entry pre-adds it as a Laboratory/Radiology line for this visit, and billing it creates
/// the real LabOrder through the existing invoice-driven flow (regression report OPD-01 — the
/// doctor's order previously never reached billing or the lab). A free-text line (no catalog
/// match) stays documentation only. The full list is replaced wholesale on every save, same
/// convention as diagnoses.
/// </summary>
internal class OpdConsultationInvestigation
{
    public Guid Id { get; private set; }
    public Guid OpdConsultationNoteId { get; private set; }
    public string Name { get; private set; } = null!;
    public OpdInvestigationDepartment Department { get; private set; }
    public OpdInvestigationPriority Priority { get; private set; }

    /// <summary>App-level reference into Masters' DiagnosticService catalog — no DB FK, validated
    /// in OpdConsultationService (same convention as OpdConsultationDiagnosis.DiagnosisId).</summary>
    public Guid? ServiceId { get; private set; }

    // Required by EF Core materialization.
    private OpdConsultationInvestigation()
    {
    }

    private OpdConsultationInvestigation(Guid id, Guid opdConsultationNoteId, string name, OpdInvestigationDepartment department, OpdInvestigationPriority priority, Guid? serviceId)
    {
        Id = id;
        OpdConsultationNoteId = opdConsultationNoteId;
        Name = name;
        Department = department;
        Priority = priority;
        ServiceId = serviceId;
    }

    public static OpdConsultationInvestigation Create(Guid opdConsultationNoteId, string name, OpdInvestigationDepartment department, OpdInvestigationPriority priority, Guid? serviceId = null)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        return new(Guid.CreateVersion7(), opdConsultationNoteId, name.Trim(), department, priority, serviceId);
    }
}
