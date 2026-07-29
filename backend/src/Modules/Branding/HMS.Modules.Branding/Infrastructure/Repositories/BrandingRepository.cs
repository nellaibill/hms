using HMS.Modules.Branding.Application.Abstractions;
using HMS.Modules.Branding.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Branding.Infrastructure.Repositories;

internal class BrandingRepository : IBrandingRepository
{
    private readonly BrandingDbContext _dbContext;

    public BrandingRepository(BrandingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<BrandingSettings?> GetAsync(CancellationToken cancellationToken)
        => _dbContext.Settings.FirstOrDefaultAsync(b => b.Id == BrandingSettings.SingletonId, cancellationToken);

    public async Task AddAsync(BrandingSettings settings, CancellationToken cancellationToken)
        => await _dbContext.Settings.AddAsync(settings, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
