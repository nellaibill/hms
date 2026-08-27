namespace HMS.Modules.Documents.Contracts;

public record DocumentResponse
{
    public Guid Id { get; init; }

    public DocumentOwnerType OwnerType { get; init; }
    public Guid OwnerId { get; init; }

    public DocumentType DocumentType { get; init; }
    public DocumentClassification Classification { get; init; }

    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }

    public DocumentStatus Status { get; init; }
    public bool IsArchived { get; init; }

    public Guid? UploadedByUserId { get; init; }

    public DateOnly? ExpiryDate { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
