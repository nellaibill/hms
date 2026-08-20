using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Application.Abstractions;

public interface ITenantFeatureService
{
    Task<Result<TenantFeaturesResponse>> GetAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<Result<TenantFeaturesResponse>> UpdateAsync(Guid tenantId, UpdateTenantFeaturesRequest request, Guid? actorId, CancellationToken cancellationToken);
}
