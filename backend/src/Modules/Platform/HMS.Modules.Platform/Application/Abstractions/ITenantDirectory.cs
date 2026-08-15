namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// The public seam every hospital-facing module uses to turn a tenant identifier into a
/// ready-to-use database connection — HMS Multi-Tenancy Phase C's single source of tenant
/// truth (platform.tenants). Implemented here (not in HMS.Api, unlike
/// <see cref="ITenantProvisioner"/>) because, unlike provisioning, resolving a tenant needs
/// no knowledge of other modules' DbContext types — just this module's own Tenant
/// aggregate — so there's no reason to push the implementation up to HMS.Api.
/// </summary>
public interface ITenantDirectory
{
    /// <summary>Used by TenantResolutionMiddleware for every request after login, keyed by
    /// the "TenantId" claim already validated on the caller's Hospital JWT.</summary>
    Task<TenantInfo?> FindByIdAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Used by AuthenticationService.LoginAsync — the one place a tenant must be
    /// resolved *before* any JWT (and therefore before any TenantId claim) exists.</summary>
    Task<TenantInfo?> FindByHospitalCodeAsync(string hospitalCode, CancellationToken cancellationToken);
}

/// <summary>
/// A trusted snapshot of a platform.tenants row, including a ready-to-use connection string
/// (already pointed at <see cref="DatabaseName"/>) — callers never construct or receive a
/// raw database name to build a connection from themselves (see Phase C's "no
/// client-controlled database selection" requirement).
/// </summary>
public sealed record TenantInfo(Guid Id, string HospitalCode, string DatabaseName, string ConnectionString, bool IsActive);
