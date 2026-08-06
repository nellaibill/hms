using HMS.Modules.Documents.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Documents.Domain;

/// <summary>
/// A single uploaded file attached to any HMS record via <see cref="OwnerType"/> +
/// <see cref="OwnerId"/> — the generic cross-module document repository described in
/// docs/modules/Documents/DocumentManagement.md. Deliberately carries no version history or
/// dynamic per-role permission grants in this iteration (see that doc's Future Enhancements).
/// </summary>
internal class Document : Entity
{
    public DocumentOwnerType OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }

    public DocumentType DocumentType { get; private set; }
    public DocumentClassification Classification { get; private set; }

    /// <summary>Server-issued relative storage path — never a client-supplied path (see
    /// Infrastructure.DocumentFileStorage). Never exposed directly over the API; content is
    /// only reachable through the authenticated GET /{id}/content endpoint.</summary>
    public string StorageKey { get; private set; } = null!;

    public string OriginalFileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public string ChecksumSha256 { get; private set; } = null!;

    public DocumentStatus Status { get; private set; }
    public bool IsArchived { get; private set; }

    public Guid? UploadedByUserId { get; private set; }

    // Required by EF Core materialization.
    private Document()
    {
    }

    private Document(
        Guid id,
        DocumentOwnerType ownerType,
        Guid ownerId,
        DocumentType documentType,
        DocumentClassification classification,
        string storageKey,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string checksumSha256,
        Guid? uploadedByUserId)
        : base(id, uploadedByUserId)
    {
        OwnerType = ownerType;
        OwnerId = ownerId;
        DocumentType = documentType;
        Classification = classification;
        StorageKey = storageKey;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        ChecksumSha256 = checksumSha256;
        UploadedByUserId = uploadedByUserId;
        Status = DocumentStatus.Pending;
        IsArchived = false;
    }

    public static Document Create(
        DocumentOwnerType ownerType,
        Guid ownerId,
        DocumentType documentType,
        DocumentClassification classification,
        string storageKey,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string checksumSha256,
        Guid? uploadedByUserId)
    {
        Guard.AgainstNullOrWhiteSpace(storageKey, nameof(storageKey));
        Guard.AgainstNullOrWhiteSpace(originalFileName, nameof(originalFileName));
        Guard.AgainstNullOrWhiteSpace(contentType, nameof(contentType));
        Guard.AgainstNullOrWhiteSpace(checksumSha256, nameof(checksumSha256));

        return new Document(
            Guid.CreateVersion7(),
            ownerType,
            ownerId,
            documentType,
            classification,
            storageKey,
            originalFileName.Trim(),
            contentType.Trim(),
            sizeBytes,
            checksumSha256,
            uploadedByUserId);
    }

    /// <summary>Called by the scan pipeline (US-9) once content passes an AV scan.</summary>
    public void MarkAvailable()
    {
        Status = DocumentStatus.Available;
        MarkUpdated(UploadedByUserId);
    }

    /// <summary>Called by the scan pipeline (US-9) when content fails an AV scan. Quarantined
    /// documents are never served by GET /{id}/content regardless of caller permissions.</summary>
    public void MarkQuarantined()
    {
        Status = DocumentStatus.Quarantined;
        MarkUpdated(UploadedByUserId);
    }

    public void Archive(Guid? actorId)
    {
        if (IsArchived)
        {
            return; // Idempotent per US-5's acceptance criteria.
        }

        IsArchived = true;
        MarkUpdated(actorId);
    }
}
