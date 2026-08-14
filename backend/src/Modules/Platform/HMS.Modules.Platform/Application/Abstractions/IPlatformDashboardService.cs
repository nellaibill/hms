using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Application.Abstractions;

public interface IPlatformDashboardService
{
    Task<PagedResult<TenantListItemResponse>> GetHospitalsAsync(TenantListQuery query, CancellationToken cancellationToken);

    Task<TenantDashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken);

    Task<Result<TenantListItemResponse>> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken);
}
