using HMS.Modules.Platform.Domain;

namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — same split as <see cref="ITenantRepository"/>.
/// </summary>
internal interface ITenantFeatureRepository
{
    Task AddRangeAsync(IEnumerable<TenantFeature> features, CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantFeature>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
