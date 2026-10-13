using System.Text.RegularExpressions;

namespace HMS.Modules.Documents.Application.Indexing;

/// <summary>
/// Turns a search question into the words worth matching literally, for the keyword fallback
/// of document search. Questions are written for semantic search ("Which antibiotics were
/// given?"), so most of their words say nothing about the passage being looked for.
/// </summary>
internal static partial class KeywordSearchTerms
{
    /// <summary>Enough for any real question; bounds the query built from them.</summary>
    internal const int MaxTerms = 12;

    /// <summary>Question words too common to say anything about a passage. Kept the same as
    /// the list the web app's AI Search tab skips when highlighting, so the words a user sees
    /// highlighted are the words that were searched for.</summary>
    private static readonly HashSet<string> StopWords =
    [
        "and", "the", "for", "with", "was", "were", "are", "has", "had", "have", "any", "all", "from", "this", "that", "there",
        "what", "which", "when", "who", "how", "did", "does", "given", "show", "list", "latest", "last", "patient", "results",
    ];

    /// <summary>Distinct lower-case words of three or more letters or digits, in question
    /// order, without the stop words — at most <see cref="MaxTerms"/>. Empty when the question
    /// has no such word.</summary>
    public static IReadOnlyList<string> From(string query) =>
        Word().Matches(query.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(word => !StopWords.Contains(word))
            .Distinct()
            .Take(MaxTerms)
            .ToList();

    // Marks (\p{M}) are part of a word in scripts such as Tamil, where vowel signs are
    // combining characters; without them one word would split into several fragments.
    [GeneratedRegex(@"[\p{L}\p{M}\p{N}]{3,}")]
    private static partial Regex Word();
}
