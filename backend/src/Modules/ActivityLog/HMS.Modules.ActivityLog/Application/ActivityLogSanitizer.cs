using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace HMS.Modules.ActivityLog.Application;

/// <summary>
/// Turns an arbitrary object into the JSON stored in OldValues/NewValues, replacing the
/// value of any property whose name looks like a credential (password, token, secret, ...)
/// with a fixed marker — at any nesting depth. Guards against a future DTO carrying a
/// secret being logged by accident; callers should still avoid passing secrets at all.
/// </summary>
internal static class ActivityLogSanitizer
{
    public const string RedactedMarker = "[REDACTED]";

    // Serialized JSON larger than this is replaced by a marker rather than stored.
    private const int MaxJsonLength = 64 * 1024;

    private static readonly string[] SensitiveFragments =
    [
        "password", "passwd", "pwd", "token", "secret", "apikey", "api_key",
        "authorization", "credential", "hash", "otp", "cvv", "cardnumber", "privatekey",
    ];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static string? Sanitize(object? value)
    {
        if (value is null)
        {
            return null;
        }

        JsonNode? node;
        try
        {
            node = JsonSerializer.SerializeToNode(value, value.GetType(), SerializerOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return "{\"_error\":\"value could not be serialized\"}";
        }

        if (node is null)
        {
            return null;
        }

        Redact(node);

        var json = node.ToJsonString();
        return json.Length > MaxJsonLength ? "{\"_truncated\":true}" : json;
    }

    internal static bool IsSensitiveKey(string key)
    {
        foreach (var fragment in SensitiveFragments)
        {
            if (key.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void Redact(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (IsSensitiveKey(key))
                    {
                        obj[key] = RedactedMarker;
                    }
                    else if (obj[key] is { } child)
                    {
                        Redact(child);
                    }
                }

                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        Redact(item);
                    }
                }

                break;
        }
    }
}
