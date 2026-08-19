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
}
