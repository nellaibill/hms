namespace HMS.Modules.Patients.Contracts;

/// <summary>One field-level validation/commit failure attached to an import row.</summary>
public record ImportRowError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public record ImportBatchResponse
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public ImportBatchStatus Status { get; init; }
    public int TotalRows { get; init; }
    public int ValidRows { get; init; }
    public int InvalidRows { get; init; }
    public int CreatedRows { get; init; }
    public int CommitFailedRows { get; init; }
    public DateTime UploadedAt { get; init; }
    public Guid? UploadedBy { get; init; }
    public DateTime? CommittedAt { get; init; }
    public Guid? CommittedBy { get; init; }
}

public record ImportRowResponse
{
    public Guid Id { get; init; }
    public int RowNumber { get; init; }
    public ImportRowStatus Status { get; init; }
    public IReadOnlyDictionary<string, string?> RawData { get; init; } = new Dictionary<string, string?>();
    public IReadOnlyList<ImportRowError> Errors { get; init; } = [];
    public Guid? CreatedPatientId { get; init; }
}

public record ImportRowListQuery
{
    public ImportRowStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public record ImportBatchListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
