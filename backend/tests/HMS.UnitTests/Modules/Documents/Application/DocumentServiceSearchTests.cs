using FluentAssertions;
using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Application.Indexing;
using HMS.Modules.Documents.Application.Security;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using HMS.Shared.Infrastructure.Ai;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Pgvector;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Application;

public class DocumentServiceSearchTests
{
    private const string Model = "BAAI/bge-m3";

    private static readonly DocumentActor Actor = new(Guid.NewGuid(), "doctor");
    private static readonly Guid PatientId = Guid.NewGuid();

    private readonly IDocumentChunkRepository _chunks = Substitute.For<IDocumentChunkRepository>();
    private readonly IAiEmbeddingProvider _provider = Substitute.For<IAiEmbeddingProvider>();
    private readonly IDocumentAccessPolicy _accessPolicy = Substitute.For<IDocumentAccessPolicy>();

    public DocumentServiceSearchTests()
    {
        _provider.IsConfigured.Returns(true);
        _provider.Model.Returns(Model);
        _accessPolicy.CanRead(Arg.Any<DocumentActor>(), Arg.Any<DocumentOwnerType>(), Arg.Any<DocumentClassification>()).Returns(true);
    }

    private DocumentService NewService() => new(
        Substitute.For<IDocumentRepository>(),
        _chunks,
        Substitute.For<IDocumentIndexer>(),
        Substitute.For<IDocumentChunkEmbedder>(),
        _provider,
        Substitute.For<IDocumentFileStorage>(),
        _accessPolicy,
        Substitute.For<IDocumentScanQueue>(),
        [],
        Substitute.For<ITenantContext>(),
        new ConfigurationBuilder().Build(),
        NullLogger<DocumentService>.Instance);

    private static DocumentChunk Chunk(int index, string content, DocumentClassification classification = DocumentClassification.Internal) =>
        DocumentChunk.Create(DocumentChunkSourceType.Document, Guid.NewGuid(), DocumentOwnerType.Patient, PatientId, classification, index, content, 4);

    private void EmbeddingSucceeds() =>
        _provider.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), EmbeddingInputKind.Query, Arg.Any<CancellationToken>())
            .Returns(Result<AiEmbeddingResult>.Success(new AiEmbeddingResult([new float[DocumentChunk.EmbeddingDimensions]], Model)));

    private void EmbeddingFails() =>
        _provider.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), EmbeddingInputKind.Query, Arg.Any<CancellationToken>())
            .Returns(Result<AiEmbeddingResult>.Failure(AiErrorCodes.RequestFailed, "The embedding service returned 402."));

    private void KeywordSearchReturns(params ChunkKeywordHit[] hits) =>
        _chunks.KeywordSearchAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DocumentOwnerType?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(hits);

    [Fact]
    public async Task ReturnsSemanticHitsWithTheirSimilarity_WhenTheQueryEmbeds()
    {
        EmbeddingSucceeds();
        _chunks.SearchAsync(Arg.Any<Vector>(), Model, DocumentOwnerType.Patient, PatientId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([new ChunkSearchHit(Chunk(2, "Amoxicillin 500 mg was started."), "discharge.pdf", 0.25)]);

        var result = await NewService().SearchAsync("Which antibiotics were given?", DocumentOwnerType.Patient, PatientId, 10, Actor, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var hit = result.Value.Should().ContainSingle().Subject;
        hit.MatchType.Should().Be(DocumentSearchMatchType.Semantic);
        hit.Similarity.Should().Be(0.75);
        hit.OriginalFileName.Should().Be("discharge.pdf");
        hit.ChunkIndex.Should().Be(2);
        await _chunks.DidNotReceiveWithAnyArgs().KeywordSearchAsync(default!, default, default, default, default);
    }

    [Fact]
    public async Task FallsBackToKeywordHits_WhenTheEmbeddingCallFails()
    {
        EmbeddingFails();
        KeywordSearchReturns(new ChunkKeywordHit(Chunk(0, "Antibiotics: amoxicillin."), "notes.docx"));

        var result = await NewService().SearchAsync("Which antibiotics were given?", DocumentOwnerType.Patient, PatientId, 10, Actor, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var hit = result.Value.Should().ContainSingle().Subject;
        hit.MatchType.Should().Be(DocumentSearchMatchType.Keyword);
        hit.Similarity.Should().BeNull();
        hit.OriginalFileName.Should().Be("notes.docx");
        hit.Content.Should().Be("Antibiotics: amoxicillin.");
        await _chunks.DidNotReceiveWithAnyArgs().SearchAsync(default!, default!, default, default, default, default);
    }

    [Fact]
    public async Task TheFallbackSearchesTheQuestionsMeaningfulWords_WithinTheSameOwnerScope()
    {
        EmbeddingFails();
        KeywordSearchReturns();

        await NewService().SearchAsync("Which antibiotics were given?", DocumentOwnerType.Patient, PatientId, 10, Actor, CancellationToken.None);

        await _chunks.Received(1).KeywordSearchAsync(
            Arg.Is<IReadOnlyList<string>>(terms => terms.SequenceEqual(new[] { "antibiotics" })),
            DocumentOwnerType.Patient, PatientId, 40, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TheFallbackAppliesTheAccessPolicyAndTheLimit()
    {
        EmbeddingFails();
        _accessPolicy.CanRead(Actor, DocumentOwnerType.Patient, DocumentClassification.Restricted).Returns(false);
        KeywordSearchReturns(
            new ChunkKeywordHit(Chunk(0, "restricted", DocumentClassification.Restricted), "legal.pdf"),
            new ChunkKeywordHit(Chunk(1, "first"), "a.pdf"),
            new ChunkKeywordHit(Chunk(2, "second"), "b.pdf"),
            new ChunkKeywordHit(Chunk(3, "third"), "c.pdf"));

        var result = await NewService().SearchAsync("amoxicillin dose", DocumentOwnerType.Patient, PatientId, 2, Actor, CancellationToken.None);

        result.Value!.Select(h => h.Content).Should().Equal("first", "second");
    }

    [Fact]
    public async Task StillReportsNotConfigured_WhenEmbeddingsAreOff()
    {
        _provider.IsConfigured.Returns(false);

        var result = await NewService().SearchAsync("antibiotics", DocumentOwnerType.Patient, PatientId, 10, Actor, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DocumentErrorCodes.EmbeddingsNotConfigured);
        await _chunks.DidNotReceiveWithAnyArgs().KeywordSearchAsync(default!, default, default, default, default);
    }

    [Fact]
    public async Task RejectsAnEmptyQuery_WithoutCallingTheEmbeddingService()
    {
        var result = await NewService().SearchAsync("   ", DocumentOwnerType.Patient, PatientId, 10, Actor, CancellationToken.None);

        result.ErrorCode.Should().Be(DocumentErrorCodes.InvalidQuery);
        await _provider.DidNotReceiveWithAnyArgs().EmbedAsync(default!, default, default);
    }
}
