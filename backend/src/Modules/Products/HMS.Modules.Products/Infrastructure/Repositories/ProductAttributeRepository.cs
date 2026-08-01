using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Products.Infrastructure.Repositories;

internal class ProductAttributeRepository : IProductAttributeRepository
{
    private readonly ProductsDbContext _dbContext;

    public ProductAttributeRepository(ProductsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductAttribute attribute, CancellationToken cancellationToken)
        => await _dbContext.ProductAttributes.AddAsync(attribute, cancellationToken);

    public Task<ProductAttribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductAttributes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductAttributes.AnyAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string attributeCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ProductAttributes.AnyAsync(a => a.AttributeCode == attributeCode && a.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<ProductAttribute> Items, int TotalCount)> GetPagedAsync(ProductAttributeListQuery query, CancellationToken cancellationToken)
    {
        var attributes = _dbContext.ProductAttributes.AsQueryable();

        if (query.IsActive.HasValue)
        {
            attributes = attributes.Where(a => a.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            attributes = attributes.Where(a => EF.Functions.ILike(a.AttributeCode, term) || EF.Functions.ILike(a.AttributeName, term));
        }

        attributes = ApplySort(attributes, query.Sort);

        var totalCount = await attributes.CountAsync(cancellationToken);
        var items = await attributes.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<ProductAttribute> ApplySort(IQueryable<ProductAttribute> attributes, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return attributes.OrderBy(a => a.AttributeName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "attributecode" => descending ? attributes.OrderByDescending(a => a.AttributeCode) : attributes.OrderBy(a => a.AttributeCode),
            "updatedat" => descending ? attributes.OrderByDescending(a => a.UpdatedAt) : attributes.OrderBy(a => a.UpdatedAt),
            _ => descending ? attributes.OrderByDescending(a => a.AttributeName) : attributes.OrderBy(a => a.AttributeName),
        };
    }
}
