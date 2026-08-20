using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Platform.Infrastructure.Repositories;

internal sealed class TenantFeatureRepository : ITenantFeatureRepository
{
    private readonly PlatformDbContext _dbContext;

    public TenantFeatureRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddRangeAsync(IEnumerable<TenantFeature> features, CancellationToken cancellationToken)
        => _dbContext.TenantFeatures.AddRangeAsync(features, cancellationToken);

    public async Task<IReadOnlyList<TenantFeature>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
        => await _dbContext.TenantFeatures.Where(f => f.TenantId == tenantId).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
