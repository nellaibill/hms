namespace HMS.Modules.ActivityLog.Application.Abstractions;

/// <summary>Ambient request facts stamped onto every audit row. Behind an interface so the
/// service is unit-testable without an HttpContext.</summary>
internal interface IActivityLogContextProvider
{
    ActivityLogContext GetCurrent();
}

internal sealed record ActivityLogContext(
    Guid? TenantId,
    Guid? UserId,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId);
