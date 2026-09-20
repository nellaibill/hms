using System.Text.Json;
using HMS.Shared.Kernel;

namespace HMS.Modules.ActivityLog.Contracts;

/// <summary>Conventional action names other modules pass to IActivityLogService.LogAsync — a
/// shared vocabulary so the Action filter is meaningful. Free-form strings are still
/// accepted; these are just the conventional ones.</summary>
public static class ActivityLogActions
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Payment = "Payment";
    public const string Void = "Void";
}

/// <summary>Conventional module names — mirror the module that performed the action.</summary>
public static class ActivityLogModules
{
    public const string Patients = "Patients";
    public const string Identity = "Identity";
    public const string Billing = "Billing";
}

/// <summary>What a caller supplies to record one audit event. TenantId, UserId (unless
/// overridden), IP address, User-Agent and CorrelationId are filled in automatically from
/// the current request context.</summary>
public record ActivityLogRequest
{
    public string Action { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? Description { get; init; }

    /// <summary>Any serializable object (typically a response DTO). Serialized to JSON and
    /// scrubbed of password/token/secret-like properties before it is stored.</summary>
    public object? OldValues { get; init; }
    public object? NewValues { get; init; }

    public bool IsSuccess { get; init; } = true;

    /// <summary>Overrides the user resolved from the request — pass the calling service's own
    /// actorId so the log stays correct when there is no HTTP context.</summary>
    public Guid? UserId { get; init; }
}

public class ActivityLogListQuery : PagedRequest
{
    /// <summary>Inclusive lower bound on CreatedAt (UTC).</summary>
    public DateTime? From { get; set; }

    /// <summary>Inclusive upper bound on CreatedAt (UTC). A date-only value (midnight) is
    /// treated as the whole day.</summary>
    public DateTime? To { get; set; }

    public Guid? UserId { get; set; }
    public string? Module { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}

public record ActivityLogResponse
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? Description { get; init; }
    public bool IsSuccess { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ActivityLogDetailResponse : ActivityLogResponse
{
    public Guid? TenantId { get; init; }
    public JsonElement? OldValues { get; init; }
    public JsonElement? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? CorrelationId { get; init; }
}
