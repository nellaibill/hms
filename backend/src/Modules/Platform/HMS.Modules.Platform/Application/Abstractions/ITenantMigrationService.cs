namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// HMS Multi-Tenancy Phase C's migration-management seam (requirement #8) — a single place
/// that knows how to bring a tenant database up to the current schema, reused by both new
/// tenant provisioning (<see cref="ITenantProvisioner"/>) and by re-syncing an existing
/// tenant after new module migrations have been added. Implemented in HMS.Api (not this
/// module) for the same reason as <see cref="ITenantProvisioner"/>: applying every module's
/// migrations requires referencing every module's DbContext type, and only HMS.Api is
/// allowed to know every module exists.
/// </summary>
public interface ITenantMigrationService
{
    /// <summary>
    /// Applies pending EF Core migrations to the database behind
    /// <paramref name="tenantConnectionString"/>, for exactly the modules named in
    /// <paramref name="enabledFeatureKeys"/> (FeatureCatalog keys) — the selective-
    /// provisioning seam (Tenant Feature/Module Management). FeatureCatalog.Mandatory is
    /// unioned in defensively inside the implementation, so no caller can ever omit it, even
    /// by bug. Idempotent per module: EF Core's Database.MigrateAsync only ever applies
    /// migrations not already recorded in that module's own migrations-history table, so
    /// calling this against an already up-to-date tenant (or re-passing a feature that was
    /// already enabled) is a safe no-op — this is what lets the same seam serve new-tenant
    /// provisioning, an operator-triggered re-migrate, and enabling a feature on an existing
    /// tenant, all without separate code paths. Deliberately never invoked per-request.
    /// </summary>
    Task MigrateAsync(string tenantConnectionString, IReadOnlyCollection<string> enabledFeatureKeys, CancellationToken cancellationToken);
}
