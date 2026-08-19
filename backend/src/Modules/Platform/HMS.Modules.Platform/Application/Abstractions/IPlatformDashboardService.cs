using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Application.Abstractions;

public interface IPlatformDashboardService
{
    Task<PagedResult<TenantListItemResponse>> GetHospitalsAsync(TenantListQuery query, CancellationToken cancellationToken);

    Task<TenantDashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken);

    Task<Result<TenantListItemResponse>> UpdateStatusAsync(Guid id, string status, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>
    /// HMS Multi-Tenancy Phase C's migration-management endpoint (requirement #8): applies
    /// every hospital module's pending EF Core migrations to this tenant's existing
    /// database — an explicit, operator-triggered action, never automatic. On failure the
    /// tenant's existing database is left exactly as it was; nothing is dropped.
    /// </summary>
    Task<Result<TenantListItemResponse>> MigrateAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Dry-run preview shown before a delete is confirmed — see
    /// TenantDeletePreviewResponse's own doc comment for why this is Platform-side data
    /// only.</summary>
    Task<Result<TenantDeletePreviewResponse>> GetDeletePreviewAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a hospital: blocks its staff from logging in (TenantDirectory's lookups
    /// already exclude soft-deleted tenants via the IsDeleted query filter) and hides it
    /// from the active hospital list, but never touches its own database — that stays fully
    /// intact and can be restored via <see cref="RestoreHospitalAsync"/>. Requires
    /// <paramref name="confirmHospitalCode"/> to match the tenant's actual hospital code
    /// (case-insensitive) — a server-enforced "type to confirm," not just a frontend dialog.
    /// </summary>
    Task<Result> DeleteHospitalAsync(Guid id, string confirmHospitalCode, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Reverses a soft-delete, restoring the hospital to the active list with its
    /// previous Status (Active/Inactive) and login access.</summary>
    Task<Result<TenantListItemResponse>> RestoreHospitalAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Lists soft-deleted hospitals, paged — the only way to find one to restore.</summary>
    Task<PagedResult<DeletedTenantListItemResponse>> GetDeletedHospitalsAsync(TenantListQuery query, CancellationToken cancellationToken);
}
