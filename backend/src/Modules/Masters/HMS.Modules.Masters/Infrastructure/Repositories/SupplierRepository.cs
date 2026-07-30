using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class SupplierRepository : ISupplierRepository
{
    private readonly MastersDbContext _dbContext;

    public SupplierRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken)
        => await _dbContext.Suppliers.AddAsync(supplier, cancellationToken);

    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string supplierCode, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Suppliers.AnyAsync(s => s.SupplierCode == supplierCode && s.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Supplier> Items, int TotalCount)> GetPagedAsync(SupplierListQuery query, CancellationToken cancellationToken)
    {
        var suppliers = _dbContext.Suppliers.AsQueryable();

        if (query.IsActive.HasValue)
        {
            suppliers = suppliers.Where(s => s.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            suppliers = suppliers.Where(s => EF.Functions.ILike(s.SupplierCode, term) || EF.Functions.ILike(s.SupplierName, term));
        }

        suppliers = ApplySort(suppliers, query.Sort);

        var totalCount = await suppliers.CountAsync(cancellationToken);
        var items = await suppliers.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Supplier> ApplySort(IQueryable<Supplier> suppliers, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return suppliers.OrderBy(s => s.SupplierName);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "suppliercode" => descending ? suppliers.OrderByDescending(s => s.SupplierCode) : suppliers.OrderBy(s => s.SupplierCode),
            "updatedat" => descending ? suppliers.OrderByDescending(s => s.UpdatedAt) : suppliers.OrderBy(s => s.UpdatedAt),
            _ => descending ? suppliers.OrderByDescending(s => s.SupplierName) : suppliers.OrderBy(s => s.SupplierName),
        };
    }
}
