using HMS.Modules.Billing.Contracts;

namespace HMS.Modules.Billing.Application;

/// <summary>
/// Turns the reasons given on recent discount or revert requests into suggestions for the
/// counter's Reason box: one per reason (ignoring case and spacing — "senior  CITIZEN " is
/// "Senior citizen"), most-used first, ties going to the most recently used, each shown in its
/// newest spelling. A reason is only suggested once it has been given twice, so a one-off typo
/// or keyboard mash ("fdyt") never is, and placeholders typed while trying the screen out
/// ("Test", "NA", ".") are never suggested at all.
/// </summary>
internal static class ReasonSuggestions
{
    /// <summary>How many recent requests are looked at — enough to learn a hospital's usual
    /// reasons without scanning its whole history every time a dialog opens.</summary>
    public const int SampleSize = 500;

    public const int MaxSuggestions = 50;

    public const int MinTimesUsed = 2;

    private const int MinLetters = 3;

    private static readonly HashSet<string> Placeholders = ["test", "testing", "demo", "dummy", "sample", "nil", "none"];

    /// <param name="newestFirst">Reasons from the most recent requests, newest first.</param>
    public static IReadOnlyList<ReasonSuggestionResponse> Rank(IEnumerable<string> newestFirst, int limit)
        => newestFirst
            .Select(r => r.Trim())
            .Where(r => r.Length > 0)
            .Select((reason, order) => (Reason: reason, Key: string.Join(' ', reason.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant(), Order: order))
            .GroupBy(r => r.Key)
            .Where(g => !IsPlaceholder(g.Key))
            .Select(g => (g.First().Reason, Count: g.Count(), Newest: g.Min(r => r.Order)))
            .Where(g => g.Count >= MinTimesUsed)
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Newest)
            .Take(Math.Clamp(limit, 1, MaxSuggestions))
            .Select(g => new ReasonSuggestionResponse { Reason = g.Reason, TimesUsed = g.Count })
            .ToList();

    private static bool IsPlaceholder(string key) => key.Count(char.IsLetter) < MinLetters || Placeholders.Contains(key);
}
