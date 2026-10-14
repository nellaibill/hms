using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// Rolls one product's batch balances up into its StockSummaryResponse. Kept as a pure
/// function of (product, balance rows, batches, today) so the rules that decide what counts
/// as available — and therefore whether the product is low or out — are testable without a
/// database or a clock.
/// </summary>
internal static class StockSummaryCalculator
{
    public static StockSummaryResponse Build(
        ProductStockProfileResponse product,
        IReadOnlyCollection<StockBalanceLine> lines,
        IReadOnlyDictionary<Guid, ProductBatchResponse> batchesById,
        DateOnly today,
        int expiringSoonWindowDays = PharmacyClock.ExpiringSoonWindowDays)
    {
        var expiringSoonCutoff = today.AddDays(expiringSoonWindowDays);

        var available = 0m;
        var expiringSoon = 0m;
        var expired = 0m;
        var onHold = 0m;
        var batchesInStock = new HashSet<Guid>();
        DateOnly? nearestExpiry = null;

        foreach (var line in lines)
        {
            // A balance whose batch no longer resolves can't be dispensed (DispenseService
            // rejects it as an invalid batch) and has no expiry to classify it by, so it is
            // left out of every bucket rather than guessed into one.
            if (line.QuantityOnHand <= 0 || !batchesById.TryGetValue(line.ProductBatchId, out var batch))
            {
                continue;
            }

            // A set, not a counter: across all stores the same batch arrives as one line per
            // store holding it.
            batchesInStock.Add(line.ProductBatchId);

            if (batch.ExpiryDate < today)
            {
                expired += line.QuantityOnHand;
            }
            else if (!batch.IsActive)
            {
                onHold += line.QuantityOnHand;
            }
            else
            {
                available += line.QuantityOnHand;

                if (batch.ExpiryDate <= expiringSoonCutoff)
                {
                    expiringSoon += line.QuantityOnHand;
                }

                if (nearestExpiry is null || batch.ExpiryDate < nearestExpiry)
                {
                    nearestExpiry = batch.ExpiryDate;
                }
            }
        }

        return new StockSummaryResponse
        {
            ProductId = product.Id,
            ProductName = product.ProductName,
            Sku = product.Sku,
            GenericName = product.GenericName,
            ReorderLevel = product.ReorderLevel,
            AvailableQuantity = available,
            ExpiringSoonQuantity = expiringSoon,
            ExpiredQuantity = expired,
            OnHoldQuantity = onHold,
            BatchCount = batchesInStock.Count,
            NearestExpiry = nearestExpiry,
            Status = ResolveStatus(available, product.ReorderLevel, hasBeenStocked: lines.Count > 0),
        };
    }

    public static bool Matches(StockSummaryResponse summary, StockSummaryFilter filter) => filter switch
    {
        StockSummaryFilter.NeedsReorder => summary.Status is StockStatus.LowStock or StockStatus.OutOfStock,
        StockSummaryFilter.ExpiringSoon => summary.ExpiringSoonQuantity > 0,
        StockSummaryFilter.Expired => summary.ExpiredQuantity > 0,
        _ => true,
    };

    private static StockStatus ResolveStatus(decimal available, decimal reorderLevel, bool hasBeenStocked)
    {
        if (available > 0)
        {
            return reorderLevel > 0 && available <= reorderLevel ? StockStatus.LowStock : StockStatus.InStock;
        }

        return hasBeenStocked || reorderLevel > 0 ? StockStatus.OutOfStock : StockStatus.NotStocked;
    }
}
