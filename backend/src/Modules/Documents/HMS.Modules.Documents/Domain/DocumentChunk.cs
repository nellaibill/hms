using HMS.Modules.Documents.Contracts;
using HMS.Shared.Kernel;
using NpgsqlTypes;
using Pgvector;

namespace HMS.Modules.Documents.Domain;

/// <summary>What a <see cref="DocumentChunk"/>'s text was extracted from. Only uploaded
/// documents are indexed today; later RAG phases add structured clinical records (OPD notes,
/// lab results, discharge summaries) as further values without a schema change.</summary>
internal enum DocumentChunkSourceType
{
    Document = 0,
}

/// <summary>
/// One retrievable slice of text for RAG search, with its embedding. Derived data — rebuilt
/// from its source whenever that source is re-indexed — so, unlike <see cref="Document"/>, it
/// deliberately doesn't extend <see cref="Entity"/>: no soft delete or update audit columns.
/// Chunks are hard-deleted with (or instead of) their source, so a soft-deleted document's
/// text can never surface in search results.
///
/// <see cref="OwnerType"/>/<see cref="OwnerId"/>/<see cref="Classification"/> are copied from
/// the source so a search can be scoped (e.g. to one patient) and filtered through
/// DocumentAccessPolicy without joining back to documents.documents.
/// </summary>
internal class DocumentChunk
{
    /// <summary>Fixed by the vector(N) column type and its HNSW index. 1024 fits every
    /// embedding model under consideration (Voyage, bge-m3/multilingual-e5-large, OpenAI
    /// text-embedding-3-* with dimensions=1024); a model with a different size needs a
    /// migration and a full re-index.</summary>
    public const int EmbeddingDimensions = 1024;

    public Guid Id { get; private set; }

    public DocumentChunkSourceType SourceType { get; private set; }
    public Guid SourceId { get; private set; }

    public DocumentOwnerType OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }
    public DocumentClassification Classification { get; private set; }

    /// <summary>0-based position within the source, so neighbouring chunks can be stitched
    /// back together for context.</summary>
    public int ChunkIndex { get; private set; }

    public string Content { get; private set; } = null!;
    public int TokenCount { get; private set; }

    /// <summary>Null until the embedding step has run — text extraction and embedding are
    /// separate steps, so a failed/retried embedding call doesn't lose the extracted text.</summary>
    public Vector? Embedding { get; private set; }

    /// <summary>The model that produced <see cref="Embedding"/>; vectors from different
    /// models aren't comparable, so a model switch means re-embedding every row whose value
    /// differs.</summary>
    public string? EmbeddingModel { get; private set; }
    public DateTime? EmbeddedAt { get; private set; }

    /// <summary>Database-generated from <see cref="Content"/> (keyword half of hybrid search);
    /// never written by the application.</summary>
    public NpgsqlTsVector SearchVector { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    // Required by EF Core materialization.
    private DocumentChunk()
    {
    }

    public static DocumentChunk Create(
        DocumentChunkSourceType sourceType,
        Guid sourceId,
        DocumentOwnerType ownerType,
        Guid ownerId,
        DocumentClassification classification,
        int chunkIndex,
        string content,
        int tokenCount)
    {
        Guard.AgainstNullOrWhiteSpace(content, nameof(content));
        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkIndex), chunkIndex, "Chunk index cannot be negative.");
        }

        return new DocumentChunk
        {
            Id = Guid.CreateVersion7(),
            SourceType = sourceType,
            SourceId = sourceId,
            OwnerType = ownerType,
            OwnerId = ownerId,
            Classification = classification,
            ChunkIndex = chunkIndex,
            Content = content,
            TokenCount = tokenCount,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void SetEmbedding(Vector embedding, string model)
    {
        Guard.AgainstNullOrWhiteSpace(model, nameof(model));
        if (embedding.Memory.Length != EmbeddingDimensions)
        {
            throw new ArgumentException($"Embedding must have {EmbeddingDimensions} dimensions, got {embedding.Memory.Length}.", nameof(embedding));
        }

        Embedding = embedding;
        EmbeddingModel = model.Trim();
        EmbeddedAt = DateTime.UtcNow;
    }
}
