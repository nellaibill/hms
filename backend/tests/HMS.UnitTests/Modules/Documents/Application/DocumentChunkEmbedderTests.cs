using FluentAssertions;
using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Application.Indexing;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using HMS.Shared.Infrastructure.Ai;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Application;

public class DocumentChunkEmbedderTests
{
    private readonly IAiEmbeddingProvider _provider = Substitute.For<IAiEmbeddingProvider>();
    private readonly IDocumentChunkRepository _chunks = Substitute.For<IDocumentChunkRepository>();

    public DocumentChunkEmbedderTests()
    {
        _provider.IsConfigured.Returns(true);
        _provider.Model.Returns("BAAI/bge-m3");
        _provider.Dimensions.Returns(DocumentChunk.EmbeddingDimensions);
    }

    private DocumentChunkEmbedder NewEmbedder() => new(_provider, _chunks, NullLogger<DocumentChunkEmbedder>.Instance);

    private static List<DocumentChunk> NewChunks(int count) => Enumerable.Range(0, count)
        .Select(i => DocumentChunk.Create(DocumentChunkSourceType.Document, Guid.NewGuid(), DocumentOwnerType.Patient, Guid.NewGuid(), DocumentClassification.Internal, i, $"chunk {i}", 2))
        .ToList();

    private void ProviderReturns(int count) =>
        _provider.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), EmbeddingInputKind.Document, Arg.Any<CancellationToken>())
            .Returns(Result<AiEmbeddingResult>.Success(new AiEmbeddingResult(
                Enumerable.Range(0, count).Select(_ => new float[DocumentChunk.EmbeddingDimensions]).ToList(), "BAAI/bge-m3")));

    [Fact]
    public async Task EmbedsEveryChunkWithTheReturnedModel_AndSaves()
    {
        var chunks = NewChunks(3);
        ProviderReturns(3);

        var embedded = await NewEmbedder().EmbedAsync(chunks, CancellationToken.None);

        embedded.Should().Be(3);
        chunks.Should().OnlyContain(c => c.Embedding != null && c.EmbeddingModel == "BAAI/bge-m3");
        await _provider.Received(1).EmbedAsync(
            Arg.Is<IReadOnlyList<string>>(texts => texts.SequenceEqual(new[] { "chunk 0", "chunk 1", "chunk 2" })), EmbeddingInputKind.Document, Arg.Any<CancellationToken>());
        await _chunks.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipsWhenEmbeddingsAreNotConfigured()
    {
        _provider.IsConfigured.Returns(false);

        var embedded = await NewEmbedder().EmbedAsync(NewChunks(2), CancellationToken.None);

        embedded.Should().Be(0);
        await _provider.DidNotReceiveWithAnyArgs().EmbedAsync(default!, default, default);
    }

    [Fact]
    public async Task SkipsWhenTheModelsDimensionsDontMatchTheColumn()
    {
        _provider.Dimensions.Returns(768);

        var embedded = await NewEmbedder().EmbedAsync(NewChunks(2), CancellationToken.None);

        embedded.Should().Be(0);
        await _provider.DidNotReceiveWithAnyArgs().EmbedAsync(default!, default, default);
    }

    [Fact]
    public async Task LeavesChunksUnembeddedWhenTheCallFails()
    {
        var chunks = NewChunks(2);
        _provider.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<EmbeddingInputKind>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiEmbeddingResult>.Failure(AiErrorCodes.RequestFailed, "boom"));

        var embedded = await NewEmbedder().EmbedAsync(chunks, CancellationToken.None);

        embedded.Should().Be(0);
        chunks.Should().OnlyContain(c => c.Embedding == null);
        await _chunks.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
