using System.Text;
using System.Text.RegularExpressions;

namespace HMS.Modules.Documents.Application.Indexing;

internal readonly record struct TextChunk(int Index, string Content, int TokenCount);

/// <summary>
/// Splits extracted document text into overlapping, roughly equal-sized chunks for RAG
/// retrieval. Boundaries prefer paragraph breaks, then sentence ends, then whitespace — a
/// chunk only ever cuts mid-word when a single "word" is itself longer than a whole chunk.
///
/// Token counts are an estimate (≈ 4 characters per token), not a real tokenizer: good enough
/// to keep chunks well inside any embedding model's input limit without tying the Documents
/// module to one provider's tokenizer.
/// </summary>
internal static partial class TextChunker
{
    public const int DefaultMaxTokens = 500;
    public const int DefaultOverlapTokens = 50;
    private const int CharsPerToken = 4;

    public static int EstimateTokens(string text) => (text.Length + CharsPerToken - 1) / CharsPerToken;

    public static IReadOnlyList<TextChunk> Chunk(string text, int maxTokens = DefaultMaxTokens, int overlapTokens = DefaultOverlapTokens)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTokens, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(overlapTokens);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(overlapTokens, maxTokens);

        var normalized = Normalize(text);
        if (normalized.Length == 0)
        {
            return [];
        }

        var maxChars = maxTokens * CharsPerToken;
        var overlapChars = overlapTokens * CharsPerToken;
        var units = SplitIntoUnits(normalized, maxChars);

        var chunks = new List<TextChunk>();
        var current = new List<string>();
        var currentLength = 0;

        foreach (var unit in units)
        {
            var addedLength = current.Count == 0 ? unit.Length : unit.Length + 1;
            if (current.Count > 0 && currentLength + addedLength > maxChars)
            {
                chunks.Add(Build(chunks.Count, current));
                (current, currentLength) = CarryOverlap(current, overlapChars, unit.Length, maxChars);
                addedLength = current.Count == 0 ? unit.Length : unit.Length + 1;
            }

            current.Add(unit);
            currentLength += addedLength;
        }

        if (current.Count > 0)
        {
            chunks.Add(Build(chunks.Count, current));
        }

        return chunks;
    }

    /// <summary>Unifies line endings, collapses runs of spaces/tabs, trims each line and
    /// caps blank-line runs at one — PDF extraction in particular produces lots of ragged
    /// whitespace that would otherwise eat into every chunk's size budget.</summary>
    private static string Normalize(string text)
    {
        var unified = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var collapsed = HorizontalWhitespace().Replace(unified, " ");
        var lines = collapsed.Split('\n').Select(line => line.Trim());
        return ExtraBlankLines().Replace(string.Join('\n', lines), "\n\n").Trim();
    }

    /// <summary>Paragraphs, with any paragraph longer than a chunk broken into sentences,
    /// and any sentence still too long broken at whitespace.</summary>
    private static List<string> SplitIntoUnits(string text, int maxChars)
    {
        var units = new List<string>();
        foreach (var paragraph in text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            if (paragraph.Length <= maxChars)
            {
                units.Add(paragraph);
                continue;
            }

            foreach (var sentence in SentenceEnd().Split(paragraph))
            {
                if (sentence.Length <= maxChars)
                {
                    units.Add(sentence);
                }
                else
                {
                    units.AddRange(HardSplit(sentence, maxChars));
                }
            }
        }

        return units;
    }

    private static IEnumerable<string> HardSplit(string text, int maxChars)
    {
        var start = 0;
        while (start < text.Length)
        {
            var length = Math.Min(maxChars, text.Length - start);
            if (start + length < text.Length)
            {
                var lastSpace = text.LastIndexOf(' ', start + length - 1, length);
                if (lastSpace > start)
                {
                    length = lastSpace - start;
                }
            }

            yield return text.Substring(start, length).Trim();
            start += length;
            while (start < text.Length && text[start] == ' ')
            {
                start++;
            }
        }
    }

    /// <summary>The trailing units of the chunk just closed, up to the overlap budget, so
    /// context spanning a boundary is retrievable from either side. Dropped entirely when
    /// even the overlap plus the next unit wouldn't fit in one chunk.</summary>
    private static (List<string> Units, int Length) CarryOverlap(List<string> previous, int overlapChars, int nextUnitLength, int maxChars)
    {
        var carried = new List<string>();
        var length = 0;
        for (var i = previous.Count - 1; i >= 0; i--)
        {
            var added = carried.Count == 0 ? previous[i].Length : previous[i].Length + 1;
            if (length + added > overlapChars)
            {
                break;
            }

            carried.Insert(0, previous[i]);
            length += added;
        }

        return length + 1 + nextUnitLength > maxChars ? ([], 0) : (carried, length);
    }

    private static TextChunk Build(int index, List<string> units)
    {
        var content = new StringBuilder().AppendJoin('\n', units).ToString();
        return new TextChunk(index, content, EstimateTokens(content));
    }

    [GeneratedRegex(@"[ \t\f\v ]+")]
    private static partial Regex HorizontalWhitespace();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExtraBlankLines();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceEnd();
}
