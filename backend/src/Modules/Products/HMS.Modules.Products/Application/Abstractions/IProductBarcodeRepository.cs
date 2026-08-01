using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Abstractions;

internal interface IProductBarcodeRepository
{
    Task AddAsync(ProductBarcode barcode, CancellationToken cancellationToken);

    Task<ProductBarcode?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByBarcodeValueAsync(string barcodeValue, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductBarcode> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductBarcodeListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
