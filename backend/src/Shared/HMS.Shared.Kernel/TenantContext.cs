namespace HMS.Shared.Kernel;

/// <summary>
/// Request-scoped (never singleton/static) holder for the current hospital tenant —
/// HMS Multi-Tenancy Phase C's single tenant-resolution seam. Populated exactly once per
/// request: either by HMS.Api's TenantResolutionMiddleware (from the authenticated
/// caller's "TenantId" JWT claim, for every request after login) or by
/// HMS.Modules.Identity.Application.AuthenticationService itself during the login call
/// (resolved from the submitted HospitalCode, before any JWT exists — see
/// AuthenticationService.LoginAsync). Every tenant-aware hospital DbContext reads
/// <see cref="ConnectionString"/> from this at the moment it's first constructed within a
/// request's DI scope, so a request can never fall back to a static/default connection.
/// Lives in Shared.Kernel (not Shared.Infrastructure) because it has zero ASP.NET Core
/// dependency — a plain scoped state bag usable from Application-layer code too.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }

    string? ConnectionString { get; }

    bool IsResolved { get; }

    /// <summary>The business-domain modules (ModuleCatalog keys) a Platform Admin has
    /// enabled for this tenant — null when unset (e.g. never populated by
    /// TenantResolutionMiddleware). AuthenticationService.LoginAsync reads this to strip
    /// permissions for disabled modules out of the issued JWT.</summary>
    IReadOnlyList<string>? EnabledModules { get; }

    /// <summary>Populates the context. Intended to be called exactly once per request/scope
    /// — by design there is no way to change an already-resolved tenant mid-request, which
    /// is what keeps tenant selection out of reach of anything a client sends.</summary>
    void SetTenant(Guid tenantId, string connectionString, IReadOnlyList<string>? enabledModules = null);
}

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public string? ConnectionString { get; private set; }

    public IReadOnlyList<string>? EnabledModules { get; private set; }

    public bool IsResolved => TenantId is not null;

    public void SetTenant(Guid tenantId, string connectionString, IReadOnlyList<string>? enabledModules = null)
    {
        TenantId = tenantId;
        ConnectionString = connectionString;
        EnabledModules = enabledModules;
    }
}
