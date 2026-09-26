using FluentAssertions;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using Pgvector;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Domain;

public class DocumentChunkTests
{
    private static DocumentChunk NewChunk(int chunkIndex = 0, string content = "HbA1c 8.2% on metformin") => DocumentChunk.Create(
        DocumentChunkSourceType.Document,
        sourceId: Guid.NewGuid(),
        DocumentOwnerType.Patient,
        ownerId: Guid.NewGuid(),
        DocumentClassification.Internal,
        chunkIndex,
        content,
        tokenCount: 6);

    private static Vector VectorOf(int dimensions) => new(new float[dimensions]);

    [Fact]
    public void Create_StartsWithoutAnEmbedding()
    {
        var chunk = NewChunk();

        chunk.Id.Should().NotBeEmpty();
        chunk.Embedding.Should().BeNull();
        chunk.EmbeddingModel.Should().BeNull();
        chunk.EmbeddedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankContent(string content)
    {
        var act = () => NewChunk(content: content);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_RejectsNegativeChunkIndex()
    {
        var act = () => NewChunk(chunkIndex: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetEmbedding_RecordsVectorAndModel()
    {
        var chunk = NewChunk();

        chunk.SetEmbedding(VectorOf(DocumentChunk.EmbeddingDimensions), " voyage-3 ");

        chunk.Embedding.Should().NotBeNull();
        chunk.EmbeddingModel.Should().Be("voyage-3");
        chunk.EmbeddedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(768)]
    [InlineData(1536)]
    public void SetEmbedding_RejectsWrongDimensions(int dimensions)
    {
        var chunk = NewChunk();

        var act = () => chunk.SetEmbedding(VectorOf(dimensions), "some-model");

        act.Should().Throw<ArgumentException>();
        chunk.Embedding.Should().BeNull();
    }
}
