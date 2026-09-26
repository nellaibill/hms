using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HMS.Modules.Documents.Application.Indexing;

namespace HMS.Modules.Documents.Infrastructure.TextExtraction;

/// <summary>Word (.docx) body text, one paragraph per line. Tables' cell paragraphs come
/// through in document order too.</summary>
internal class DocxTextExtractor : IDocumentTextExtractor
{
    public IReadOnlyCollection<string> ContentTypes { get; } = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"];

    public async Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = await BufferAsync(content, cancellationToken);
        using var document = WordprocessingDocument.Open(buffer, isEditable: false);

        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        var text = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            text.AppendLine(paragraph.InnerText);
        }

        return text.ToString();
    }

    internal static async Task<MemoryStream> BufferAsync(Stream content, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }
}

/// <summary>
/// Excel (.xlsx) cell values, one row per line with cells separated by " | " and a heading
/// per sheet, so a row's values stay together in one chunk. " | " rather than a tab because
/// the chunker collapses horizontal whitespace, which would run the columns together. Uses
/// each cell's formatted value (what a person sees), not raw formulas.
/// </summary>
internal class XlsxTextExtractor : IDocumentTextExtractor
{
    public IReadOnlyCollection<string> ContentTypes { get; } = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"];

    public async Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = await DocxTextExtractor.BufferAsync(content, cancellationToken);
        using var workbook = new XLWorkbook(buffer);

        var text = new StringBuilder();
        foreach (var sheet in workbook.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            text.AppendLine($"Sheet: {sheet.Name}");
            foreach (var row in sheet.RowsUsed())
            {
                var values = row.CellsUsed().Select(cell => cell.GetFormattedString().Trim()).Where(value => value.Length > 0);
                var line = string.Join(" | ", values);
                if (line.Length > 0)
                {
                    text.AppendLine(line);
                }
            }

            text.AppendLine();
        }

        return text.ToString();
    }
}
