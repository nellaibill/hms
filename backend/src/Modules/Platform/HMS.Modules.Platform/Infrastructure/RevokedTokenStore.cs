using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Platform.Infrastructure;

internal sealed class RevokedTokenStore : IRevokedTokenStore
{
    private readonly PlatformDbContext _dbContext;

    public RevokedTokenStore(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken)
    {
        // Idempotent: logging out twice with the same token (e.g. a retried request) must
        // not throw on the second attempt.
        var exists = await _dbContext.RevokedTokens.AnyAsync(t => t.Jti == jti, cancellationToken);
        if (exists)
        {
            return;
        }

        await _dbContext.RevokedTokens.AddAsync(RevokedToken.Revoke(jti, expiresAt), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken) =>
        _dbContext.RevokedTokens.AnyAsync(t => t.Jti == jti, cancellationToken);
}
