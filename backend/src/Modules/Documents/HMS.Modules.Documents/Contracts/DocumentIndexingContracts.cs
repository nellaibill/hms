namespace HMS.Modules.Documents.Contracts;

/// <summary>One stored RAG chunk of a document (GET /api/v1/documents/{id}/chunks) — for
/// inspecting what indexing produced; the embedding vector itself isn't returned.</summary>
public record DocumentChunkResponse
{
    public int ChunkIndex { get; init; }
    public string Content { get; init; } = string.Empty;
    public int TokenCount { get; init; }
    public bool HasEmbedding { get; init; }
    public string? EmbeddingModel { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Outcome of indexing one document. <see cref="SkipReason"/> is set when it
/// produced no chunks: NotAvailable, UnsupportedContentType (e.g. an image, until OCR) or
/// NoText (e.g. a scanned PDF with no text layer).</summary>
public record DocumentIndexResponse
{
    public Guid DocumentId { get; init; }
    public int ChunkCount { get; init; }
    public string? SkipReason { get; init; }
}

/// <summary>Outcome of one backfill batch. When <see cref="NextCreatedAfter"/> is set there
/// may be more to do — call again passing it as <c>createdAfter</c>.</summary>
public record DocumentIndexBackfillResponse
{
    public int Processed { get; init; }
    public int Indexed { get; init; }
    public int ChunksCreated { get; init; }
    public int Skipped { get; init; }
    public int Failed { get; init; }
    public DateTime? NextCreatedAfter { get; init; }
}
