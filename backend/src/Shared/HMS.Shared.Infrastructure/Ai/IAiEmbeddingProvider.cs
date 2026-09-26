using HMS.Shared.Kernel;

namespace HMS.Shared.Infrastructure.Ai;

/// <summary>Whether text is being stored for retrieval or used as a search query. Some
/// embedding models (e.g. the e5 family) expect different prefixes for the two; for others
/// (bge-m3, OpenAI) both are configured empty.</summary>
public enum EmbeddingInputKind
{
    Document,
    Query,
}

public sealed record AiEmbeddingResult(IReadOnlyList<float[]> Vectors, string Model);

/// <summary>
/// Turns text into embedding vectors for RAG. Configured under <c>Ai:Embeddings</c>,
/// separately from <c>Ai:Provider</c>: the chat providers used for extraction
/// (Anthropic in particular) don't offer embeddings, and embeddings for patient text may need
/// to stay on a local model even where generation doesn't. A missing configuration is a
/// Result.Failure with <see cref="AiErrorCodes.NotConfigured"/>, so callers can skip
/// embedding (chunks stay searchable by keyword) rather than fail.
/// </summary>
public interface IAiEmbeddingProvider
{
    bool IsConfigured { get; }

    /// <summary>The model id stored alongside each vector; vectors from different models are
    /// not comparable.</summary>
    string Model { get; }

    int Dimensions { get; }

    /// <summary>One vector per input, in input order. Batches internally.</summary>
    Task<Result<AiEmbeddingResult>> EmbedAsync(IReadOnlyList<string> inputs, EmbeddingInputKind kind, CancellationToken cancellationToken);
}
