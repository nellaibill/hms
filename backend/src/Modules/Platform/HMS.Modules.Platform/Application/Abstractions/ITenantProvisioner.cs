using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// Public seam implemented in HMS.Api (HMS.Api.Provisioning.TenantProvisioningService),
/// not inside this module — provisioning a tenant database requires referencing every
/// hospital module's public DbContext type to run its migrations, and only HMS.Api is
/// allowed to know every module exists (see docs/DecisionLog.md's SaaS provisioning ADR).
/// This module depends only on this abstraction; it never references EF Core or any other
/// module's DbContext directly.
/// </summary>
public interface ITenantProvisioner
{
    /// <summary>
    /// Creates a new isolated PostgreSQL database, applies every existing HMIS module's
    /// migrations to it, and seeds the hospital's first Super Admin account. On any failure
    /// after the database is created, the implementation drops it before returning — the
    /// caller never has to clean up a partially-provisioned database.
    /// </summary>
    Task<Result<TenantProvisionResult>> ProvisionAsync(TenantProvisionRequest request, CancellationToken cancellationToken);
}

public sealed record TenantProvisionRequest(
    string HospitalName,
    string SuperAdminUsername,
    string SuperAdminFirstName,
    string SuperAdminLastName,
    string SuperAdminEmail,
    string SuperAdminPhoneNumber,
    string SuperAdminPassword);

public sealed record TenantProvisionResult(string DatabaseName);

/// <summary>Shared with HMS.Modules.Platform.Application.PlatformErrorCodes.ProvisioningFailed.</summary>
public static class TenantProvisioningErrorCodes
{
    public const string Failed = "PLATFORM.PROVISIONING_FAILED";
}
