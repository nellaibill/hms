using HMS.Shared.Kernel;

namespace HMS.Modules.ActivityLog.Domain;

/// <summary>
/// One immutable audit row. Deliberately does NOT derive from <c>HMS.Shared.Kernel.Entity</c>:
/// audit records are append-only, so the base type's soft-delete / UpdatedAt / Restore
/// members must not exist here at all. There is no mutating method, and nothing in the
/// module (repository, service, controller) can update or delete a row.
/// </summary>
internal class ActivityLogEntry
{
    public Guid Id { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = null!;
    public string Module { get; private set; } = null!;
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string? Description { get; private set; }

    /// <summary>Already-sanitized JSON (see ActivityLogSanitizer) — never raw caller data.</summary>
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }
    public bool IsSuccess { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Required by EF Core materialization.
    private ActivityLogEntry()
    {
    }

    public static ActivityLogEntry Create(
        Guid? tenantId,
        Guid? userId,
        string action,
        string module,
        string? entityType,
        string? entityId,
        string? description,
        string? oldValues,
        string? newValues,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        bool isSuccess)
    {
        Guard.AgainstNullOrWhiteSpace(action, nameof(action));
        Guard.AgainstNullOrWhiteSpace(module, nameof(module));

        return new ActivityLogEntry
        {
            // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = userId,
            Action = Truncate(action.Trim(), 100)!,
            Module = Truncate(module.Trim(), 100)!,
            EntityType = Truncate(entityType?.Trim(), 100),
            EntityId = Truncate(entityId?.Trim(), 100),
            Description = Truncate(description?.Trim(), 1000),
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = Truncate(ipAddress, 45),
            UserAgent = Truncate(userAgent, 512),
            CorrelationId = Truncate(correlationId, 100),
            IsSuccess = isSuccess,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
