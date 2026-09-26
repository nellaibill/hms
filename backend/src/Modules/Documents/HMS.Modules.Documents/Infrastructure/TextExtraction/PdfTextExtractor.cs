using System.Text;
using HMS.Modules.Documents.Application.Indexing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace HMS.Modules.Documents.Infrastructure.TextExtraction;

/// <summary>
/// Reads a PDF's embedded text layer. A scanned PDF (page images, no text layer) yields
/// little or no text — DocumentIndexer then records it as having nothing to index until OCR
/// exists.
/// </summary>
internal class PdfTextExtractor : IDocumentTextExtractor
{
    public IReadOnlyCollection<string> ContentTypes { get; } = ["application/pdf"];

    public async Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken)
    {
        // PdfPig needs a seekable stream; stored files are read through a FileStream today,
        // but don't rely on that.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        using var pdf = PdfDocument.Open(buffer);
        var text = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Content-order extraction (rather than page.Text) keeps words and lines
            // separated, so chunk boundaries and keyword search see real words.
            text.AppendLine(ContentOrderTextExtractor.GetText(page));
            text.AppendLine();
        }

        return text.ToString();
    }
}
