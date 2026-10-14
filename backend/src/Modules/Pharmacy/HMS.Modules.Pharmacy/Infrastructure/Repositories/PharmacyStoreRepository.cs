using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Pharmacy.Infrastructure.Repositories;

internal class PharmacyStoreRepository : IPharmacyStoreRepository
{
    private readonly PharmacyDbContext _dbContext;

    public PharmacyStoreRepository(PharmacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PharmacyStore store, CancellationToken cancellationToken)
        => await _dbContext.Stores.AddAsync(store, cancellationToken);

    public Task<PharmacyStore?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<PharmacyStore?> GetDefaultAsync(CancellationToken cancellationToken)
        => _dbContext.Stores.Where(s => s.IsDefault).OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<PharmacyStore>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.Stores.OrderByDescending(s => s.IsDefault).ThenBy(s => s.Name).ToListAsync(cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken)
        => _dbContext.Stores.AnyAsync(s => s.Code == code, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
