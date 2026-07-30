using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Products.Infrastructure.Repositories;

internal class ProductBarcodeRepository : IProductBarcodeRepository
{
    private readonly ProductsDbContext _dbContext;

    public ProductBarcodeRepository(ProductsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductBarcode barcode, CancellationToken cancellationToken)
        => await _dbContext.ProductBarcodes.AddAsync(barcode, cancellationToken);

    public Task<ProductBarcode?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ProductBarcodes.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> ExistsByBarcodeValueAsync(string barcodeValue, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ProductBarcodes.AnyAsync(b => b.BarcodeValue == barcodeValue && b.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<ProductBarcode> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductBarcodeListQuery query, CancellationToken cancellationToken)
    {
        var barcodes = _dbContext.ProductBarcodes.Where(b => b.ProductId == productId);

        if (query.IsActive.HasValue)
        {
            barcodes = barcodes.Where(b => b.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            barcodes = barcodes.Where(b => EF.Functions.ILike(b.BarcodeValue, term) || EF.Functions.ILike(b.BarcodeType, term));
        }

        barcodes = barcodes.OrderByDescending(b => b.IsPrimary).ThenBy(b => b.BarcodeType);

        var totalCount = await barcodes.CountAsync(cancellationToken);
        var items = await barcodes.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
