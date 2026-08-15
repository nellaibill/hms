using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Platform.Infrastructure.Repositories;

internal sealed class PlatformUserRepository : IPlatformUserRepository
{
    private readonly PlatformDbContext _dbContext;

    public PlatformUserRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PlatformUser user, CancellationToken cancellationToken)
        => await _dbContext.PlatformUsers.AddAsync(user, cancellationToken);

    public Task<PlatformUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _dbContext.PlatformUsers.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
