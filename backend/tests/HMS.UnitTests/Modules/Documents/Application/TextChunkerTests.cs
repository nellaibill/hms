using FluentAssertions;
using HMS.Modules.Documents.Application.Indexing;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Application;

public class TextChunkerTests
{
    private static string Words(int count, string word = "word") => string.Join(' ', Enumerable.Repeat(word, count));

    [Theory]
    [InlineData("")]
    [InlineData("   \n\n \t ")]
    public void Chunk_ReturnsNothingForBlankText(string text)
    {
        TextChunker.Chunk(text).Should().BeEmpty();
    }

    [Fact]
    public void Chunk_KeepsShortTextAsOneNormalizedChunk()
    {
        var chunks = TextChunker.Chunk("  Patient:\tJohn   Doe \r\n\r\n\r\n\r\nHbA1c  8.2%  ");

        chunks.Should().ContainSingle();
        chunks[0].Index.Should().Be(0);
        chunks[0].Content.Should().Be("Patient: John Doe\nHbA1c 8.2%");
        chunks[0].TokenCount.Should().Be(TextChunker.EstimateTokens(chunks[0].Content));
    }

    [Fact]
    public void Chunk_SplitsLongTextWithinTheSizeLimit_AndNumbersChunksInOrder()
    {
        var paragraphs = Enumerable.Range(1, 40).Select(i => $"Paragraph {i}. " + Words(40));
        var text = string.Join("\n\n", paragraphs);

        var chunks = TextChunker.Chunk(text, maxTokens: 100, overlapTokens: 10);

        chunks.Should().HaveCountGreaterThan(1);
        chunks.Select(c => c.Index).Should().Equal(Enumerable.Range(0, chunks.Count));
        chunks.Should().OnlyContain(c => c.Content.Length <= 100 * 4);
        chunks.Should().OnlyContain(c => c.Content.Trim().Length > 0);
    }

    [Fact]
    public void Chunk_LosesNoParagraph()
    {
        var paragraphs = Enumerable.Range(1, 30).Select(i => $"Marker{i:D2} " + Words(30)).ToList();

        var chunks = TextChunker.Chunk(string.Join("\n\n", paragraphs), maxTokens: 120, overlapTokens: 20);

        var all = string.Join('\n', chunks.Select(c => c.Content));
        foreach (var i in Enumerable.Range(1, 30))
        {
            all.Should().Contain($"Marker{i:D2}");
        }
    }

    [Fact]
    public void Chunk_RepeatsTheLastParagraphOfAChunkAtTheStartOfTheNext()
    {
        // Short paragraphs so one fits in the overlap budget.
        var paragraphs = Enumerable.Range(1, 20).Select(i => $"Line {i:D2} " + Words(8)).ToList();

        var chunks = TextChunker.Chunk(string.Join("\n\n", paragraphs), maxTokens: 60, overlapTokens: 15);

        chunks.Should().HaveCountGreaterThan(1);
        for (var i = 1; i < chunks.Count; i++)
        {
            var previousLastLine = chunks[i - 1].Content.Split('\n')[^1];
            chunks[i].Content.Should().StartWith(previousLastLine);
        }
    }

    [Fact]
    public void Chunk_BreaksAnOversizedParagraphAtSentencesThenWhitespace()
    {
        var sentence = Words(30) + ".";
        var paragraph = string.Join(' ', Enumerable.Repeat(sentence, 10)) + " " + Words(300, "x");

        var chunks = TextChunker.Chunk(paragraph, maxTokens: 50, overlapTokens: 0);

        chunks.Should().OnlyContain(c => c.Content.Length <= 50 * 4);
        chunks.Should().OnlyContain(c => !c.Content.StartsWith(' ') && !c.Content.EndsWith(' '));
        string.Concat(chunks.Select(c => c.Content)).Replace("\n", "").Replace(" ", "")
            .Should().Be(paragraph.Replace(" ", ""));
    }

    [Fact]
    public void Chunk_HardSplitsAWordLongerThanAChunk()
    {
        var chunks = TextChunker.Chunk(new string('a', 1000), maxTokens: 50, overlapTokens: 0);

        chunks.Should().HaveCount(5);
        chunks.Should().OnlyContain(c => c.Content.Length == 200);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 100)]
    [InlineData(100, -1)]
    public void Chunk_RejectsInvalidSizes(int maxTokens, int overlapTokens)
    {
        var act = () => TextChunker.Chunk("text", maxTokens, overlapTokens);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
