namespace HMS.Modules.Documents.Application.Indexing;

/// <summary>
/// Pulls plain text out of one stored file format for RAG indexing. One implementation per
/// format family (Infrastructure/TextExtraction), all registered; DocumentIndexer picks the
/// one whose <see cref="ContentTypes"/> lists the document's content type. Images
/// (JPEG/PNG) have no extractor yet — scanned reports need OCR, a later step — so they're
/// skipped rather than indexed as empty.
/// </summary>
internal interface IDocumentTextExtractor
{
    IReadOnlyCollection<string> ContentTypes { get; }

    Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken);
}
