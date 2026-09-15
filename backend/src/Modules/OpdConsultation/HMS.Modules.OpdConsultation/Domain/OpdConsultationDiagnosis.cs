using HMS.Modules.OpdConsultation.Contracts;

namespace HMS.Modules.OpdConsultation.Domain;

/// <summary>
/// One diagnosis line, always owned by exactly one OpdConsultationNote — a real, same-schema
/// DB foreign key (unlike DiagnosisId, an app-level reference into Masters' Diagnosis catalog,
/// validated by OpdConsultationService before this is created — same convention
/// ConsultantConsultationType.ConsultationTypeId already uses for its own cross-module Masters
/// reference). The full list is replaced wholesale on every save
/// (OpdConsultationNote.ReplaceDiagnoses), no separate per-line CRUD.
/// </summary>
internal class OpdConsultationDiagnosis
{
    public Guid Id { get; private set; }
    public Guid OpdConsultationNoteId { get; private set; }
    public Guid DiagnosisId { get; private set; }
    public OpdDiagnosisType Type { get; private set; }

    // Required by EF Core materialization.
    private OpdConsultationDiagnosis()
    {
    }

    private OpdConsultationDiagnosis(Guid id, Guid opdConsultationNoteId, Guid diagnosisId, OpdDiagnosisType type)
    {
        Id = id;
        OpdConsultationNoteId = opdConsultationNoteId;
        DiagnosisId = diagnosisId;
        Type = type;
    }

    public static OpdConsultationDiagnosis Create(Guid opdConsultationNoteId, Guid diagnosisId, OpdDiagnosisType type)
        => new(Guid.CreateVersion7(), opdConsultationNoteId, diagnosisId, type);
}
