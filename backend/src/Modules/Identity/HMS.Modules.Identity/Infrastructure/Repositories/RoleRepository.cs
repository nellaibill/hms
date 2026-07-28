using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Identity.Infrastructure.Repositories;

internal class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _dbContext;

    public RoleRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken)
        => await _dbContext.Roles.AddAsync(role, cancellationToken);

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();

        return _dbContext.Roles.FirstOrDefaultAsync(
            r => r.Name == normalized,
            cancellationToken);
    }

    public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();

        return _dbContext.Roles.FirstOrDefaultAsync(
            r => r.Code == normalized,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Role> Items, int TotalCount)> GetPagedAsync(
        RoleListQuery query,
        CancellationToken cancellationToken)
    {
        var roles = _dbContext.Roles.AsQueryable();

        if (query.IsActive.HasValue)
        {
            roles = roles.Where(r => r.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";

            roles = roles.Where(r =>
                EF.Functions.ILike(r.Name, term) ||
                EF.Functions.ILike(r.Code, term) ||
                EF.Functions.ILike(r.Description!, term));
        }

        roles = ApplySort(roles, query.Sort);

        var totalCount = await roles.CountAsync(cancellationToken);

        var items = await roles
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Role> ApplySort(IQueryable<Role> roles, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return roles.OrderByDescending(r => r.CreatedAt);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "name" =>
                descending
                    ? roles.OrderByDescending(r => r.Name)
                    : roles.OrderBy(r => r.Name),

            "code" =>
                descending
                    ? roles.OrderByDescending(r => r.Code)
                    : roles.OrderBy(r => r.Code),

            "displayorder" =>
                descending
                    ? roles.OrderByDescending(r => r.DisplayOrder)
                    : roles.OrderBy(r => r.DisplayOrder),

            "createdat" =>
                descending
                    ? roles.OrderByDescending(r => r.CreatedAt)
                    : roles.OrderBy(r => r.CreatedAt),

            _ => roles.OrderByDescending(r => r.CreatedAt),
        };
    }
}