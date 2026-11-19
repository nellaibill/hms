using System.Globalization;
using HMS.Modules.ActivityLog.Contracts;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Activity log entries for price and charge changes in Masters (ADR-143): lab tests and scans,
/// lab packages, consultants' consultation charges, consultation types, and procedure /
/// injection / file prices. Each entry names the record (EntityReference) and says what changed
/// ("price ₹50 → ₹60"); the full before/after records go in Old/New values.
/// </summary>
internal static class MastersActivityLog
{
    public const string Module = "Masters";

    public static ActivityLogRequest Request(
        string action,
        string entityType,
        Guid entityId,
        string reference,
        string description,
        object? oldValues,
        object? newValues,
        Guid? actorId) => new()
        {
            Action = action,
            Module = Module,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            EntityReference = reference,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = actorId,
        };

    /// <summary>"₹1,300" (paise only when there are any); "not set" for a missing amount.</summary>
    public static string Money(decimal? amount)
        => amount is null ? "not set" : "₹" + amount.Value.ToString("#,##0.##", CultureInfo.InvariantCulture);

    /// <summary>" — price ₹50 → ₹60, running cost ₹35 → ₹40" for the amounts that changed, or ""
    /// when none did.</summary>
    public static string Changes(params (string Label, decimal? Before, decimal? After)[] fields)
        => Join(fields.Where(f => f.Before != f.After).Select(f => $"{f.Label} {Money(f.Before)} → {Money(f.After)}"));

    /// <summary>" — a, b, c", or "" for no parts.</summary>
    public static string Join(IEnumerable<string> parts)
    {
        var list = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        return list.Count == 0 ? string.Empty : " — " + string.Join(", ", list);
    }

    /// <summary>"deactivated" / "reactivated" when the active switch changed, else "".</summary>
    public static string Status(bool before, bool after)
        => before == after ? string.Empty : after ? "reactivated" : "deactivated";
}
