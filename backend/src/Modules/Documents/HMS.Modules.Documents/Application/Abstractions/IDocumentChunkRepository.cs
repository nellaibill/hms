using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using Pgvector;

namespace HMS.Modules.Documents.Application.Abstractions;

/// <summary>One vector-search hit: the chunk, its document's file name, and cosine distance
/// (0 = same direction, 2 = opposite).</summary>
internal sealed record ChunkSearchHit(DocumentChunk Chunk, string OriginalFileName, double Distance);

/// <summary>Persistence for documents.document_chunks. Chunks are always written and removed
/// per source as a set, never edited one by one — except for filling in their embedding.</summary>
internal interface IDocumentChunkRepository
{
    /// <summary>Atomically swaps the source's existing chunks for <paramref name="chunks"/>
    /// (an empty list just clears them). The new chunks stay tracked, so an embedding set on
    /// them afterwards is persisted by <see cref="SaveChangesAsync"/>.</summary>
    Task ReplaceForSourceAsync(DocumentChunkSourceType sourceType, Guid sourceId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken);

    Task DeleteForSourceAsync(DocumentChunkSourceType sourceType, Guid sourceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentChunk>> GetForSourceAsync(DocumentChunkSourceType sourceType, Guid sourceId, CancellationToken cancellationToken);

    /// <summary>Available, non-deleted documents of an indexable content type with no chunks
    /// yet, oldest first, created strictly after <paramref name="createdAfter"/> — the
    /// backfill's work list. The cursor matters: a document that yields no text (a scanned
    /// PDF) stays chunk-less, so without it every backfill call would start on the same ones.</summary>
    Task<IReadOnlyList<(Guid Id, DateTime CreatedAt)>> GetUnindexedDocumentsAsync(IReadOnlyCollection<string> contentTypes, DateTime? createdAfter, int limit, CancellationToken cancellationToken);

    /// <summary>Tracked chunks with no embedding, or one from a model other than
    /// <paramref name="model"/> — the embed backfill's work list.</summary>
    Task<IReadOnlyList<DocumentChunk>> GetUnembeddedAsync(string model, int limit, CancellationToken cancellationToken);

    /// <summary>Nearest chunks to <paramref name="query"/> by cosine distance, among chunks
    /// embedded with <paramref name="model"/> whose document isn't deleted, optionally
    /// scoped to one owner type / owner.</summary>
    Task<IReadOnlyList<ChunkSearchHit>> SearchAsync(Vector query, string model, DocumentOwnerType? ownerType, Guid? ownerId, int limit, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
