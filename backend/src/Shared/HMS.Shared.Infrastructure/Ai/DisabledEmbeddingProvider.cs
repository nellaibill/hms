using HMS.Shared.Kernel;

namespace HMS.Shared.Infrastructure.Ai;

/// <summary>Registered when <c>Ai:Embeddings:Provider</c> is unset: embedding is skipped and
/// chunks stay keyword-searchable only.</summary>
internal sealed class DisabledEmbeddingProvider : IAiEmbeddingProvider
{
    public bool IsConfigured => false;

    public string Model => string.Empty;

    public int Dimensions => 0;

    public Task<Result<AiEmbeddingResult>> EmbedAsync(IReadOnlyList<string> inputs, EmbeddingInputKind kind, CancellationToken cancellationToken) =>
        Task.FromResult(Result<AiEmbeddingResult>.Failure(AiErrorCodes.NotConfigured, "Embeddings aren't enabled (Ai:Embeddings:Provider)."));
}
