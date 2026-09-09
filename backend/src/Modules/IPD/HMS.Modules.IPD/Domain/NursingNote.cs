using HMS.Modules.IPD.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A single timestamped nursing note recorded against an Admission — chronological nursing
/// log. Append-only, same convention as NursingAssessment/VitalsReading/AdmissionCharge: a
/// correction is a new note, not an edit to clinical history. No navigation collection on
/// Admission itself.
/// </summary>
internal class NursingNote : Entity
{
    public Guid AdmissionId { get; private set; }

    /// <summary>When the note pertains to — distinct from CreatedAt.</summary>
    public DateTime NoteDateTime { get; private set; }

    public NursingShift Shift { get; private set; }
    public string? Observation { get; private set; }
    public string? Intervention { get; private set; }
    public string? PatientResponse { get; private set; }
    public string? Remarks { get; private set; }

    /// <summary>Opaque reference into identity.users — same no-FK convention as Discharge
    /// Summary's sign-off fields.</summary>
    public Guid? RecordedByUserId { get; private set; }

    // Required by EF Core materialization.
    private NursingNote()
    {
    }

    private NursingNote(
        Guid id,
        Guid admissionId,
        DateTime noteDateTime,
        NursingShift shift,
        string? observation,
        string? intervention,
        string? patientResponse,
        string? remarks,
        Guid? recordedByUserId,
        Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        NoteDateTime = noteDateTime;
        Shift = shift;
        Observation = Normalize(observation);
        Intervention = Normalize(intervention);
        PatientResponse = Normalize(patientResponse);
        Remarks = Normalize(remarks);
        RecordedByUserId = recordedByUserId;
    }

    public static NursingNote Create(
        Guid admissionId,
        DateTime noteDateTime,
        NursingShift shift,
        string? observation,
        string? intervention,
        string? patientResponse,
        string? remarks,
        Guid? recordedByUserId,
        Guid? createdBy)
    {
        return new NursingNote(
            Guid.CreateVersion7(),
            admissionId,
            noteDateTime,
            shift,
            observation,
            intervention,
            patientResponse,
            remarks,
            recordedByUserId,
            createdBy);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
