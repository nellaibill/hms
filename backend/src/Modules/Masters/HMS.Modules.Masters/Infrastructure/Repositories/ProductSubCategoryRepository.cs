using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class ProductSubCategoryRepository : IProductSubCategoryRepository
{
    private readonly MastersDbContext _dbContext;

    public ProductSubCategoryRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductSubCategory subCategory, CancellationToken cancellationToken)
        => await _dbContext.ProductSubCategories.AddAsync(subCategory, cancellationToken);

    public Task<ProductSubCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductSubCategories.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string subCategoryCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ProductSubCategories.AnyAsync(s => s.SubCategoryCode == subCategoryCode && s.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<ProductSubCategory> Items, int TotalCount)> GetPagedAsync(ProductSubCategoryListQuery query, CancellationToken cancellationToken)
    {
        var subCategories = _dbContext.ProductSubCategories.AsQueryable();

        if (query.IsActive.HasValue)
        {
            subCategories = subCategories.Where(s => s.IsActive == query.IsActive.Value);
        }

        if (query.CategoryId.HasValue)
        {
            subCategories = subCategories.Where(s => s.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            subCategories = subCategories.Where(s => EF.Functions.ILike(s.SubCategoryCode, term) || EF.Functions.ILike(s.SubCategoryName, term));
        }

        subCategories = ApplySort(subCategories, query.Sort);

        var totalCount = await subCategories.CountAsync(cancellationToken);
        var items = await subCategories.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<ProductSubCategory> ApplySort(IQueryable<ProductSubCategory> subCategories, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return subCategories.OrderBy(s => s.SubCategoryName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "subcategorycode" => descending ? subCategories.OrderByDescending(s => s.SubCategoryCode) : subCategories.OrderBy(s => s.SubCategoryCode),
            "sortorder" => descending ? subCategories.OrderByDescending(s => s.SortOrder) : subCategories.OrderBy(s => s.SortOrder),
            "updatedat" => descending ? subCategories.OrderByDescending(s => s.UpdatedAt) : subCategories.OrderBy(s => s.UpdatedAt),
            _ => descending ? subCategories.OrderByDescending(s => s.SubCategoryName) : subCategories.OrderBy(s => s.SubCategoryName),
        };
    }
}
