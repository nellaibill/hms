using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class ProgressNoteMappingExtensions
{
    public static ProgressNoteResponse ToResponse(this ProgressNote note) => new()
    {
        Id = note.Id,
        AdmissionId = note.AdmissionId,
        NoteDateTime = note.NoteDateTime,
        ClinicalCondition = note.ClinicalCondition,
        Progress = note.Progress,
        Diagnosis = note.Diagnosis,
        Assessment = note.Assessment,
        Plan = note.Plan,
        Instructions = note.Instructions,
        AuthorUserId = note.AuthorUserId,
        CreatedAt = note.CreatedAt,
    };
}
