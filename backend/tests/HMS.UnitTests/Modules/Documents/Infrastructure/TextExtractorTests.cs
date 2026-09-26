using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using HMS.Modules.Documents.Infrastructure.TextExtraction;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Infrastructure;

/// <summary>Each extractor against a real file of its format, generated in memory.</summary>
public class TextExtractorTests
{
    [Fact]
    public async Task Pdf_ExtractsTextFromEveryPage()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4).AddText("Discharge summary for John Doe", 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);
        builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4).AddText("HbA1c 8.2 percent", 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);
        using var pdf = new MemoryStream(builder.Build());

        var text = await new PdfTextExtractor().ExtractAsync(pdf, CancellationToken.None);

        text.Should().Contain("Discharge summary for John Doe").And.Contain("HbA1c 8.2 percent");
    }

    [Fact]
    public async Task Docx_ExtractsParagraphsInOrder()
    {
        using var docx = new MemoryStream();
        using (var document = WordprocessingDocument.Create(docx, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(new Run(new Text("Consent for surgery"))),
                new Paragraph(new Run(new Text("Procedure: laparoscopic cholecystectomy")))));
        }

        docx.Position = 0;
        var text = await new DocxTextExtractor().ExtractAsync(docx, CancellationToken.None);

        text.Should().Be("Consent for surgery" + Environment.NewLine + "Procedure: laparoscopic cholecystectomy" + Environment.NewLine);
    }

    [Fact]
    public async Task Xlsx_ExtractsEachSheetsRowsAsPipeSeparatedLines()
    {
        using var xlsx = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Labs");
            sheet.Cell(1, 1).Value = "Test";
            sheet.Cell(1, 2).Value = "Result";
            sheet.Cell(2, 1).Value = "HbA1c";
            sheet.Cell(2, 2).Value = 8.2;
            workbook.SaveAs(xlsx);
        }

        xlsx.Position = 0;
        var text = await new XlsxTextExtractor().ExtractAsync(xlsx, CancellationToken.None);

        text.Should().Contain("Sheet: Labs").And.Contain("Test | Result").And.Contain("HbA1c | 8.2");
    }
}
