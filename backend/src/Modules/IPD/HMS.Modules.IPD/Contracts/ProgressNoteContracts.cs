namespace HMS.Modules.IPD.Contracts;

public record CreateProgressNoteRequest
{
    public DateTime NoteDateTime { get; init; }
    public string? ClinicalCondition { get; init; }
    public string? Progress { get; init; }
    public string? Diagnosis { get; init; }
    public string? Assessment { get; init; }
    public string? Plan { get; init; }
    public string? Instructions { get; init; }
    public Guid? AuthorUserId { get; init; }
}

public record ProgressNoteResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }
    public DateTime NoteDateTime { get; init; }
    public string? ClinicalCondition { get; init; }
    public string? Progress { get; init; }
    public string? Diagnosis { get; init; }
    public string? Assessment { get; init; }
    public string? Plan { get; init; }
    public string? Instructions { get; init; }
    public Guid? AuthorUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
