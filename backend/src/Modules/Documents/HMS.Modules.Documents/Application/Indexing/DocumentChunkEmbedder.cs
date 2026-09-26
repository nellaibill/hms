using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Domain;
using HMS.Shared.Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Documents.Application.Indexing;

internal interface IDocumentChunkEmbedder
{
    /// <summary>Embeds and saves the given (tracked) chunks. Returns how many were embedded —
    /// 0 when embeddings aren't configured or the call failed; that failure is logged, never
    /// thrown, so chunks stay stored (and keyword-searchable) with a null embedding for a
    /// later embed backfill.</summary>
    Task<int> EmbedAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken);
}

internal class DocumentChunkEmbedder : IDocumentChunkEmbedder
{
    private readonly IAiEmbeddingProvider _provider;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly ILogger<DocumentChunkEmbedder> _logger;

    public DocumentChunkEmbedder(IAiEmbeddingProvider provider, IDocumentChunkRepository chunkRepository, ILogger<DocumentChunkEmbedder> logger)
    {
        _provider = provider;
        _chunkRepository = chunkRepository;
        _logger = logger;
    }

    public async Task<int> EmbedAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        if (chunks.Count == 0 || !_provider.IsConfigured)
        {
            return 0;
        }

        if (_provider.Dimensions != DocumentChunk.EmbeddingDimensions)
        {
            _logger.LogError("Embedding model {Model} is configured for {Dimensions} dimensions but document_chunks.embedding is vector({Expected}); not embedding.", _provider.Model, _provider.Dimensions, DocumentChunk.EmbeddingDimensions);
            return 0;
        }

        var result = await _provider.EmbedAsync(chunks.Select(c => c.Content).ToList(), EmbeddingInputKind.Document, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Embedding {Count} chunks failed ({ErrorCode}: {Error}); they stay unembedded.", chunks.Count, result.ErrorCode, result.Error);
            return 0;
        }

        for (var i = 0; i < chunks.Count; i++)
        {
            chunks[i].SetEmbedding(new Pgvector.Vector(result.Value!.Vectors[i]), result.Value.Model);
        }

        await _chunkRepository.SaveChangesAsync(cancellationToken);
        return chunks.Count;
    }
}
