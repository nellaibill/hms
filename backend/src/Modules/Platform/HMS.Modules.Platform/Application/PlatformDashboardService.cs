using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Application;

internal sealed class PlatformDashboardService : IPlatformDashboardService
{
    private readonly ITenantRepository _tenantRepository;

    public PlatformDashboardService(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<PagedResult<TenantListItemResponse>> GetHospitalsAsync(TenantListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _tenantRepository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<TenantListItemResponse>(items.Select(ToResponse).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<TenantDashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken)
    {
        var (total, active, inactive) = await _tenantRepository.GetCountsAsync(cancellationToken);
        return new TenantDashboardStatsResponse { Total = total, Active = active, Inactive = inactive };
    }

    public async Task<Result<TenantListItemResponse>> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TenantStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            return Result<TenantListItemResponse>.Failure(PlatformErrorCodes.InvalidStatus, $"'{status}' is not a valid status.");
        }

        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantListItemResponse>.Failure(PlatformErrorCodes.NotFound, "No hospital was found for the given id.");
        }

        tenant.SetStatus(parsedStatus, updatedBy: null);
        await _tenantRepository.SaveChangesAsync(cancellationToken);

        return Result<TenantListItemResponse>.Success(ToResponse(tenant));
    }

    private static TenantListItemResponse ToResponse(Tenant tenant) => new()
    {
        Id = tenant.Id,
        HospitalName = tenant.HospitalName,
        HospitalCode = tenant.HospitalCode,
        DatabaseName = tenant.DatabaseName,
        Status = tenant.Status.ToString(),
        CreatedAt = tenant.CreatedAt,
    };
}
