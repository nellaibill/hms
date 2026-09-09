using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class NursingNoteMappingExtensions
{
    public static NursingNoteResponse ToResponse(this NursingNote note) => new()
    {
        Id = note.Id,
        AdmissionId = note.AdmissionId,
        NoteDateTime = note.NoteDateTime,
        Shift = note.Shift,
        Observation = note.Observation,
        Intervention = note.Intervention,
        PatientResponse = note.PatientResponse,
        Remarks = note.Remarks,
        RecordedByUserId = note.RecordedByUserId,
        CreatedAt = note.CreatedAt,
    };
}
