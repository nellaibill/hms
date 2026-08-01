using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class ProductGroupRepository : IProductGroupRepository
{
    private readonly MastersDbContext _dbContext;

    public ProductGroupRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductGroup group, CancellationToken cancellationToken)
        => await _dbContext.ProductGroups.AddAsync(group, cancellationToken);

    public Task<ProductGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string groupCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ProductGroups.AnyAsync(g => g.GroupCode == groupCode && g.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<ProductGroup> Items, int TotalCount)> GetPagedAsync(ProductGroupListQuery query, CancellationToken cancellationToken)
    {
        var groups = _dbContext.ProductGroups.AsQueryable();

        if (query.IsActive.HasValue)
        {
            groups = groups.Where(g => g.IsActive == query.IsActive.Value);
        }

        if (query.SubCategoryId.HasValue)
        {
            groups = groups.Where(g => g.SubCategoryId == query.SubCategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            groups = groups.Where(g => EF.Functions.ILike(g.GroupCode, term) || EF.Functions.ILike(g.GroupName, term));
        }

        groups = ApplySort(groups, query.Sort);

        var totalCount = await groups.CountAsync(cancellationToken);
        var items = await groups.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<ProductGroup> ApplySort(IQueryable<ProductGroup> groups, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return groups.OrderBy(g => g.GroupName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "groupcode" => descending ? groups.OrderByDescending(g => g.GroupCode) : groups.OrderBy(g => g.GroupCode),
            "sortorder" => descending ? groups.OrderByDescending(g => g.SortOrder) : groups.OrderBy(g => g.SortOrder),
            "updatedat" => descending ? groups.OrderByDescending(g => g.UpdatedAt) : groups.OrderBy(g => g.UpdatedAt),
            _ => descending ? groups.OrderByDescending(g => g.GroupName) : groups.OrderBy(g => g.GroupName),
        };
    }
}
