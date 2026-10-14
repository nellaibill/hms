using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>One batch a store can sell from right now, with its per-unit figures.</summary>
internal sealed record SellableBatch(ProductBatchResponse Batch, decimal Quantity, decimal UnitPrice, decimal UnitMrp, decimal? UnitCost);

/// <summary>A product and the batches of it a store can sell, earliest expiry first.</summary>
internal sealed record SellableItem(ProductResponse Product, IReadOnlyList<SellableBatch> Batches)
{
    public decimal AvailableQuantity => Batches.Sum(b => b.Quantity);
}

internal interface ISellableStockReader
{
    /// <summary>For each product: the store's active, unexpired batches that have stock,
    /// first-expiring first — the order they should leave the shelf.</summary>
    Task<IReadOnlyDictionary<Guid, SellableItem>> ReadAsync(IReadOnlyCollection<ProductResponse> products, Guid storeId, CancellationToken cancellationToken);
}

/// <summary>
/// Answers "what can this store sell of these products, and at what price?" in two queries
/// however many products are asked about. The counter's item search and the sale itself both
/// read stock through here, so the batch the search shows first is the batch the sale takes.
/// </summary>
internal class SellableStockReader : ISellableStockReader
{
    private readonly IPharmacyStockBalanceRepository _balanceRepository;
    private readonly IProductBatchService _productBatchService;

    public SellableStockReader(IPharmacyStockBalanceRepository balanceRepository, IProductBatchService productBatchService)
    {
        _balanceRepository = balanceRepository;
        _productBatchService = productBatchService;
    }

    public async Task<IReadOnlyDictionary<Guid, SellableItem>> ReadAsync(IReadOnlyCollection<ProductResponse> products, Guid storeId, CancellationToken cancellationToken)
    {
        if (products.Count == 0)
        {
            return new Dictionary<Guid, SellableItem>();
        }

        var lines = await _balanceRepository.GetLinesAsync(products.Select(p => p.Id).ToList(), storeId, cancellationToken);
        var inStock = lines.Where(l => l.QuantityOnHand > 0).ToList();
        var batches = await _productBatchService.GetLookupAsync(inStock.Select(l => l.ProductBatchId), cancellationToken);
        var today = PharmacyClock.Today();

        var result = new Dictionary<Guid, SellableItem>(products.Count);
        foreach (var product in products)
        {
            var sellable = inStock
                .Where(l => l.ProductId == product.Id)
                .Select(l => (Line: l, Batch: batches.GetValueOrDefault(l.ProductBatchId)))
                .Where(x => x.Batch is { IsActive: true } && x.Batch.ExpiryDate >= today)
                .OrderBy(x => x.Batch!.ExpiryDate).ThenBy(x => x.Batch!.BatchNo, StringComparer.Ordinal)
                .Select(x => new SellableBatch(
                    x.Batch!,
                    x.Line.QuantityOnHand,
                    PharmacyPricing.UnitPrice(product, x.Batch!),
                    PharmacyPricing.UnitMrp(product, x.Batch!),
                    PharmacyPricing.UnitCost(product, x.Batch!)))
                .ToList();

            result[product.Id] = new SellableItem(product, sellable);
        }

        return result;
    }
}
