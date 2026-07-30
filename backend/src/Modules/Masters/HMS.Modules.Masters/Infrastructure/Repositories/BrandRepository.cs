using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class BrandRepository : IBrandRepository
{
    private readonly MastersDbContext _dbContext;

    public BrandRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Brand brand, CancellationToken cancellationToken)
        => await _dbContext.Brands.AddAsync(brand, cancellationToken);

    public Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string brandCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Brands.AnyAsync(b => b.BrandCode == brandCode && b.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Brand> Items, int TotalCount)> GetPagedAsync(BrandListQuery query, CancellationToken cancellationToken)
    {
        var brands = _dbContext.Brands.AsQueryable();

        if (query.IsActive.HasValue)
        {
            brands = brands.Where(b => b.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            brands = brands.Where(b => EF.Functions.ILike(b.BrandCode, term) || EF.Functions.ILike(b.BrandName, term));
        }

        brands = ApplySort(brands, query.Sort);

        var totalCount = await brands.CountAsync(cancellationToken);
        var items = await brands.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Brand> ApplySort(IQueryable<Brand> brands, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return brands.OrderBy(b => b.BrandName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "brandcode" => descending ? brands.OrderByDescending(b => b.BrandCode) : brands.OrderBy(b => b.BrandCode),
            "updatedat" => descending ? brands.OrderByDescending(b => b.UpdatedAt) : brands.OrderBy(b => b.UpdatedAt),
            _ => descending ? brands.OrderByDescending(b => b.BrandName) : brands.OrderBy(b => b.BrandName),
        };
    }
}
