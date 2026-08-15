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
    /// Applies every current hospital module's pending EF Core migrations to the database
    /// behind <paramref name="tenantConnectionString"/>. Idempotent: EF Core's
    /// Database.MigrateAsync only ever applies migrations not already recorded in that
    /// module's own migrations-history table, so calling this against an already
    /// up-to-date tenant is a safe no-op — this is deliberately never invoked per-request,
    /// only from provisioning and from an explicit operator-triggered migrate action.
    /// </summary>
    Task MigrateAsync(string tenantConnectionString, CancellationToken cancellationToken);
}
