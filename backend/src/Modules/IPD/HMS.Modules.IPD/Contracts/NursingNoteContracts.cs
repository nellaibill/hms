namespace HMS.Modules.IPD.Contracts;

public record CreateNursingNoteRequest
{
    public DateTime NoteDateTime { get; init; }
    public NursingShift Shift { get; init; }
    public string? Observation { get; init; }
    public string? Intervention { get; init; }
    public string? PatientResponse { get; init; }
    public string? Remarks { get; init; }
    public Guid? RecordedByUserId { get; init; }
}

public record NursingNoteResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }
    public DateTime NoteDateTime { get; init; }
    public NursingShift Shift { get; init; }
    public string? Observation { get; init; }
    public string? Intervention { get; init; }
    public string? PatientResponse { get; init; }
    public string? Remarks { get; init; }
    public Guid? RecordedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
