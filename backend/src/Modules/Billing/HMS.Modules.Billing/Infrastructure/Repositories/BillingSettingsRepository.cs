using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Billing.Infrastructure.Repositories;

internal class BillingSettingsRepository : IBillingSettingsRepository
{
    private readonly BillingDbContext _dbContext;

    public BillingSettingsRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Oldest first, in case two first-ever saves raced and both inserted a row: every reader
    // then agrees on the same one.
    public Task<BillingSettings?> GetAsync(CancellationToken cancellationToken)
        => _dbContext.BillingSettings.OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(BillingSettings settings, CancellationToken cancellationToken)
        => await _dbContext.BillingSettings.AddAsync(settings, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
