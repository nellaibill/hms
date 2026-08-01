using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class ProductCategoryRepository : IProductCategoryRepository
{
    private readonly MastersDbContext _dbContext;

    public ProductCategoryRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductCategory category, CancellationToken cancellationToken)
        => await _dbContext.ProductCategories.AddAsync(category, cancellationToken);

    public Task<ProductCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductCategories.AnyAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string categoryCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ProductCategories.AnyAsync(c => c.CategoryCode == categoryCode && c.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<ProductCategory> Items, int TotalCount)> GetPagedAsync(ProductCategoryListQuery query, CancellationToken cancellationToken)
    {
        var categories = _dbContext.ProductCategories.AsQueryable();

        if (query.IsActive.HasValue)
        {
            categories = categories.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            categories = categories.Where(c => EF.Functions.ILike(c.CategoryCode, term) || EF.Functions.ILike(c.CategoryName, term));
        }

        categories = ApplySort(categories, query.Sort);

        var totalCount = await categories.CountAsync(cancellationToken);
        var items = await categories.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<ProductCategory> ApplySort(IQueryable<ProductCategory> categories, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return categories.OrderBy(c => c.CategoryName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "categorycode" => descending ? categories.OrderByDescending(c => c.CategoryCode) : categories.OrderBy(c => c.CategoryCode),
            "sortorder" => descending ? categories.OrderByDescending(c => c.SortOrder) : categories.OrderBy(c => c.SortOrder),
            "updatedat" => descending ? categories.OrderByDescending(c => c.UpdatedAt) : categories.OrderBy(c => c.UpdatedAt),
            _ => descending ? categories.OrderByDescending(c => c.CategoryName) : categories.OrderBy(c => c.CategoryName),
        };
    }
}
