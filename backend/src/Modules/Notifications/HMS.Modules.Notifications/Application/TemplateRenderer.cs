using System.Text.RegularExpressions;

namespace HMS.Modules.Notifications.Application;

/// <summary>
/// Substitutes <c>{{Key}}</c> tokens in a NotificationTemplate's BodyTemplate/Subject
/// against a caller-supplied data dictionary. Deliberately minimal — no conditionals, no
/// loops, no nested paths; just flat key lookup, matching the placeholder examples in the
/// design doc ("Hello {{PatientName}}"). An unmatched token is left as literal text rather
/// than throwing or silently blanking it, so a typo'd placeholder is visible in the
/// rendered output instead of producing a confusing gap.
/// </summary>
internal static partial class TemplateRenderer
{
    public static string Render(string template, IReadOnlyDictionary<string, string>? placeholders)
    {
        if (placeholders is null || placeholders.Count == 0)
        {
            return template;
        }

        return PlaceholderPattern().Replace(template, match =>
            placeholders.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex PlaceholderPattern();
}
