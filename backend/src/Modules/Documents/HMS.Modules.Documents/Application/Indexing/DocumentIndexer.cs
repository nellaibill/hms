using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Documents.Application.Indexing;

/// <summary>Why a document ended up with no chunks — surfaced by the reindex/backfill
/// endpoints so "nothing indexed" is explainable rather than silent.</summary>
internal enum DocumentIndexSkipReason
{
    NotAvailable,
    UnsupportedContentType,
    NoText,
}

internal readonly record struct DocumentIndexResult(int ChunkCount, DocumentIndexSkipReason? SkipReason);

internal interface IDocumentIndexer
{
    /// <summary>Content types some registered extractor can read — the backfill's filter.</summary>
    IReadOnlyCollection<string> IndexableContentTypes { get; }

    /// <summary>(Re)builds the document's chunks from its stored file, replacing any it
    /// already had. Embeddings are not computed here — chunks are written with a null
    /// embedding for the embedding step to fill in.</summary>
    Task<DocumentIndexResult> IndexAsync(Document document, CancellationToken cancellationToken);
}

/// <summary>
/// RAG phase 1's extract → chunk → store step for uploaded documents. Runs after the virus
/// scan marks a document Available (DocumentScanBackgroundService), and on demand via the
/// reindex/backfill endpoints.
/// </summary>
internal class DocumentIndexer : IDocumentIndexer
{
    /// <summary>Upper bound on extracted text kept per document (~500k tokens). Guards memory
    /// and the chunk table against a pathological file (e.g. a huge spreadsheet export); the
    /// tail beyond it isn't indexed and a warning is logged.</summary>
    internal const int MaxExtractedChars = 2_000_000;

    private readonly IReadOnlyDictionary<string, IDocumentTextExtractor> _extractorsByContentType;
    private readonly IDocumentFileStorage _fileStorage;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly ILogger<DocumentIndexer> _logger;

    public DocumentIndexer(
        IEnumerable<IDocumentTextExtractor> extractors,
        IDocumentFileStorage fileStorage,
        IDocumentChunkRepository chunkRepository,
        ILogger<DocumentIndexer> logger)
    {
        _extractorsByContentType = extractors
            .SelectMany(e => e.ContentTypes.Select(contentType => (contentType, e)))
            .ToDictionary(pair => pair.contentType, pair => pair.e, StringComparer.OrdinalIgnoreCase);
        _fileStorage = fileStorage;
        _chunkRepository = chunkRepository;
        _logger = logger;
    }

    public IReadOnlyCollection<string> IndexableContentTypes => _extractorsByContentType.Keys.ToList();

    public async Task<DocumentIndexResult> IndexAsync(Document document, CancellationToken cancellationToken)
    {
        if (document.Status != DocumentStatus.Available)
        {
            // Pending/Quarantined content must never be read into search — and a document
            // that was re-quarantined loses whatever it had.
            await _chunkRepository.DeleteForSourceAsync(DocumentChunkSourceType.Document, document.Id, cancellationToken);
            return new DocumentIndexResult(0, DocumentIndexSkipReason.NotAvailable);
        }

        if (!_extractorsByContentType.TryGetValue(document.ContentType, out var extractor))
        {
            _logger.LogInformation("Document {DocumentId} ({ContentType}) has no text extractor; not indexed.", document.Id, document.ContentType);
            await _chunkRepository.DeleteForSourceAsync(DocumentChunkSourceType.Document, document.Id, cancellationToken);
            return new DocumentIndexResult(0, DocumentIndexSkipReason.UnsupportedContentType);
        }

        string text;
        await using (var content = await _fileStorage.OpenReadAsync(document.StorageKey, cancellationToken))
        {
            text = await extractor.ExtractAsync(content, cancellationToken);
        }

        if (text.Length > MaxExtractedChars)
        {
            _logger.LogWarning("Document {DocumentId} extracted to {Length} characters; indexing only the first {Max}.", document.Id, text.Length, MaxExtractedChars);
            text = text[..MaxExtractedChars];
        }

        var chunks = TextChunker.Chunk(text)
            .Select(chunk => DocumentChunk.Create(
                DocumentChunkSourceType.Document,
                document.Id,
                document.OwnerType,
                document.OwnerId,
                document.Classification,
                chunk.Index,
                chunk.Content,
                chunk.TokenCount))
            .ToList();

        await _chunkRepository.ReplaceForSourceAsync(DocumentChunkSourceType.Document, document.Id, chunks, cancellationToken);

        if (chunks.Count == 0)
        {
            _logger.LogInformation("Document {DocumentId} has no extractable text (scanned?); not indexed.", document.Id);
            return new DocumentIndexResult(0, DocumentIndexSkipReason.NoText);
        }

        _logger.LogInformation("Document {DocumentId} indexed into {ChunkCount} chunks.", document.Id, chunks.Count);
        return new DocumentIndexResult(chunks.Count, null);
    }
}
