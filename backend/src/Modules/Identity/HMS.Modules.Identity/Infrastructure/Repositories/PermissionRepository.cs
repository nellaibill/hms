using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Identity.Infrastructure.Repositories;

/// <summary>
/// Provides read access to the system permission catalog.
/// Permissions are system-defined and cannot be created,
/// updated or deleted through the application.
/// </summary>
internal sealed class PermissionRepository : IPermissionRepository
{
    private readonly IdentityDbContext _dbContext;

    public PermissionRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Permission>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Permissions
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetActiveByKeysAsync(
        IEnumerable<string> permissionKeys,
        CancellationToken cancellationToken)
    {
        var keys = permissionKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct()
            .ToList();

        if (keys.Count == 0)
        {
            return [];
        }

        return await _dbContext.Permissions
            .Where(p =>
                p.IsActive &&
                !p.IsDeleted &&
                keys.Contains(p.Key))
            .ToListAsync(cancellationToken);
    }
}