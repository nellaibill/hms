namespace HMS.Modules.Documents.Application.Abstractions;

/// <summary>
/// Persists an uploaded file's bytes and returns a server-issued storage key — mirrors
/// HMS.Modules.Patients.Application.Abstractions.IPatientFileStorage's local-disk approach
/// (see docs/DecisionLog.md's file-upload ADR: no blob storage/CDN until there's a real
/// need). Unlike Patients' uploads, the stored key returned here is <em>never</em> served via
/// `app.UseStaticFiles()` — Documents' content is only reachable through the authenticated
/// GET /api/v1/documents/{id}/content endpoint (see docs/ApiStandards.md §10 and
/// docs/modules/Documents/DocumentManagement.md for why).
/// </summary>
internal interface IDocumentFileStorage
{
    /// <summary>Persists <paramref name="content"/> to disk in a single pass, computing its
    /// SHA-256 checksum as it copies rather than re-reading the stream a second time.</summary>
    Task<SavedFile> SaveAsync(Guid documentId, string fileName, Stream content, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

internal readonly record struct SavedFile(string StorageKey, string ChecksumSha256);
