using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A single timestamped doctor clinical note recorded against an Admission — chronological
/// progress-note log. Append-only, same convention as VitalsReading/AdmissionCharge: a
/// correction is a new note, not an edit to clinical history. No navigation collection on
/// Admission itself.
/// </summary>
internal class ProgressNote : Entity
{
    public Guid AdmissionId { get; private set; }

    /// <summary>When the note pertains to — distinct from CreatedAt.</summary>
    public DateTime NoteDateTime { get; private set; }

    public string? ClinicalCondition { get; private set; }
    public string? Progress { get; private set; }
    public string? Diagnosis { get; private set; }
    public string? Assessment { get; private set; }
    public string? Plan { get; private set; }
    public string? Instructions { get; private set; }

    /// <summary>Opaque reference into identity.users — same no-FK convention as Discharge
    /// Summary's sign-off fields.</summary>
    public Guid? AuthorUserId { get; private set; }

    // Required by EF Core materialization.
    private ProgressNote()
    {
    }

    private ProgressNote(
        Guid id,
        Guid admissionId,
        DateTime noteDateTime,
        string? clinicalCondition,
        string? progress,
        string? diagnosis,
        string? assessment,
        string? plan,
        string? instructions,
        Guid? authorUserId,
        Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        NoteDateTime = noteDateTime;
        ClinicalCondition = Normalize(clinicalCondition);
        Progress = Normalize(progress);
        Diagnosis = Normalize(diagnosis);
        Assessment = Normalize(assessment);
        Plan = Normalize(plan);
        Instructions = Normalize(instructions);
        AuthorUserId = authorUserId;
    }

    public static ProgressNote Create(
        Guid admissionId,
        DateTime noteDateTime,
        string? clinicalCondition,
        string? progress,
        string? diagnosis,
        string? assessment,
        string? plan,
        string? instructions,
        Guid? authorUserId,
        Guid? createdBy)
    {
        return new ProgressNote(
            Guid.CreateVersion7(),
            admissionId,
            noteDateTime,
            clinicalCondition,
            progress,
            diagnosis,
            assessment,
            plan,
            instructions,
            authorUserId,
            createdBy);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
