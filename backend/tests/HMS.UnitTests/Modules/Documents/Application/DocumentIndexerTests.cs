using System.Text;
using FluentAssertions;
using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Application.Indexing;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Application;

public class DocumentIndexerTests
{
    private const string Pdf = "application/pdf";

    private readonly IDocumentFileStorage _fileStorage = Substitute.For<IDocumentFileStorage>();
    private readonly IDocumentChunkRepository _chunks = Substitute.For<IDocumentChunkRepository>();
    private readonly IDocumentTextExtractor _pdfExtractor = Substitute.For<IDocumentTextExtractor>();

    public DocumentIndexerTests()
    {
        _pdfExtractor.ContentTypes.Returns([Pdf]);
        _fileStorage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("file"))));
    }

    private DocumentIndexer NewIndexer() => new([_pdfExtractor], _fileStorage, _chunks, NullLogger<DocumentIndexer>.Instance);

    private static Document NewDocument(string contentType = Pdf, bool available = true)
    {
        var document = Document.Create(
            DocumentOwnerType.Patient,
            Guid.NewGuid(),
            DocumentType.Report,
            DocumentClassification.Confidential,
            "2026/09/key.pdf",
            "report.pdf",
            contentType,
            sizeBytes: 4,
            checksumSha256: new string('a', 64),
            uploadedByUserId: null);
        if (available)
        {
            document.MarkAvailable();
        }

        return document;
    }

    private void ExtractedTextIs(string text) =>
        _pdfExtractor.ExtractAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(text);

    [Fact]
    public async Task IndexAsync_StoresChunksCarryingTheDocumentsOwnerAndClassification()
    {
        var document = NewDocument();
        ExtractedTextIs("HbA1c 8.2% on metformin.");
        IReadOnlyList<DocumentChunk>? stored = null;
        await _chunks.ReplaceForSourceAsync(DocumentChunkSourceType.Document, document.Id, Arg.Do<IReadOnlyList<DocumentChunk>>(c => stored = c), Arg.Any<CancellationToken>());

        var result = await NewIndexer().IndexAsync(document, CancellationToken.None);

        result.Should().Be(new DocumentIndexResult(1, null));
        stored.Should().ContainSingle();
        var chunk = stored![0];
        chunk.SourceId.Should().Be(document.Id);
        chunk.OwnerType.Should().Be(DocumentOwnerType.Patient);
        chunk.OwnerId.Should().Be(document.OwnerId);
        chunk.Classification.Should().Be(DocumentClassification.Confidential);
        chunk.Content.Should().Be("HbA1c 8.2% on metformin.");
        chunk.Embedding.Should().BeNull();
    }

    [Fact]
    public async Task IndexAsync_SkipsAndClearsADocumentThatIsNotAvailable()
    {
        var document = NewDocument(available: false);

        var result = await NewIndexer().IndexAsync(document, CancellationToken.None);

        result.SkipReason.Should().Be(DocumentIndexSkipReason.NotAvailable);
        await _chunks.Received(1).DeleteForSourceAsync(DocumentChunkSourceType.Document, document.Id, Arg.Any<CancellationToken>());
        await _fileStorage.DidNotReceiveWithAnyArgs().OpenReadAsync(default!, default);
    }

    [Fact]
    public async Task IndexAsync_SkipsAContentTypeWithNoExtractor()
    {
        var document = NewDocument(contentType: "image/png");

        var result = await NewIndexer().IndexAsync(document, CancellationToken.None);

        result.SkipReason.Should().Be(DocumentIndexSkipReason.UnsupportedContentType);
        await _chunks.Received(1).DeleteForSourceAsync(DocumentChunkSourceType.Document, document.Id, Arg.Any<CancellationToken>());
        await _fileStorage.DidNotReceiveWithAnyArgs().OpenReadAsync(default!, default);
    }

    [Fact]
    public async Task IndexAsync_ReportsNoTextForAFileWithoutATextLayer()
    {
        var document = NewDocument();
        ExtractedTextIs("  \n\n ");

        var result = await NewIndexer().IndexAsync(document, CancellationToken.None);

        result.Should().Be(new DocumentIndexResult(0, DocumentIndexSkipReason.NoText));
        await _chunks.Received(1).ReplaceForSourceAsync(
            DocumentChunkSourceType.Document, document.Id, Arg.Is<IReadOnlyList<DocumentChunk>>(c => c.Count == 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IndexAsync_TruncatesExtractedTextBeyondTheCap()
    {
        var document = NewDocument();
        ExtractedTextIs(new string('a', DocumentIndexer.MaxExtractedChars + 10_000));
        IReadOnlyList<DocumentChunk>? stored = null;
        await _chunks.ReplaceForSourceAsync(DocumentChunkSourceType.Document, document.Id, Arg.Do<IReadOnlyList<DocumentChunk>>(c => stored = c), Arg.Any<CancellationToken>());

        await NewIndexer().IndexAsync(document, CancellationToken.None);

        stored!.Sum(c => c.Content.Length).Should().Be(DocumentIndexer.MaxExtractedChars);
    }

    [Fact]
    public void IndexableContentTypes_AreThoseOfTheRegisteredExtractors()
    {
        NewIndexer().IndexableContentTypes.Should().BeEquivalentTo([Pdf]);
    }
}
