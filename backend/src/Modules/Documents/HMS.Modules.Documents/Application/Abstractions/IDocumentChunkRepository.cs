using HMS.Modules.Documents.Domain;

namespace HMS.Modules.Documents.Application.Abstractions;

/// <summary>Persistence for documents.document_chunks. Chunks are always written and removed
/// per source as a set, never edited one by one.</summary>
internal interface IDocumentChunkRepository
{
    /// <summary>Atomically swaps the source's existing chunks for <paramref name="chunks"/>
    /// (an empty list just clears them).</summary>
    Task ReplaceForSourceAsync(DocumentChunkSourceType sourceType, Guid sourceId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken);

    Task DeleteForSourceAsync(DocumentChunkSourceType sourceType, Guid sourceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentChunk>> GetForSourceAsync(DocumentChunkSourceType sourceType, Guid sourceId, CancellationToken cancellationToken);

    /// <summary>Available, non-deleted documents of an indexable content type with no chunks
    /// yet, oldest first, created strictly after <paramref name="createdAfter"/> — the
    /// backfill's work list. The cursor matters: a document that yields no text (a scanned
    /// PDF) stays chunk-less, so without it every backfill call would start on the same ones.</summary>
    Task<IReadOnlyList<(Guid Id, DateTime CreatedAt)>> GetUnindexedDocumentsAsync(IReadOnlyCollection<string> contentTypes, DateTime? createdAfter, int limit, CancellationToken cancellationToken);
}
