using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// The one place a price per stock unit is worked out. Stock is held in units and every
/// price in Products is per pack (Product.SetDrugProfile), so each figure here is a pack
/// price divided by the product's units per pack — and a batch's own price, where it has
/// one, always wins over the product's.
/// </summary>
internal static class PharmacyPricing
{
    /// <summary>What one unit sells for: the batch's selling rate, else its MRP, else the
    /// product's selling price.</summary>
    public static decimal UnitPrice(ProductResponse product, ProductBatchResponse batch)
        => PerUnit(batch.SellingRate ?? batch.Mrp ?? product.SellingPrice, product);

    /// <summary>The printed MRP of one unit: the batch's MRP, else the product's.</summary>
    public static decimal UnitMrp(ProductResponse product, ProductBatchResponse batch)
        => PerUnit(batch.Mrp ?? product.Mrp, product);

    /// <summary>What one unit cost: the batch's purchase rate, else the product's cost price.
    /// Null when neither is recorded — an unknown cost is not a cost of zero.</summary>
    public static decimal? UnitCost(ProductResponse product, ProductBatchResponse batch)
    {
        var packCost = batch.PurchaseRate ?? (product.CostPrice > 0 ? product.CostPrice : null);
        return packCost is { } cost ? PerUnit(cost, product) : null;
    }

    /// <summary>A line's amount in rupees and paise.</summary>
    public static decimal LineTotal(decimal quantity, decimal unitPrice)
        => Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);

    // Four decimals: a strip of 15 at ₹20 is ₹1.3333 a tablet, and 15 of those must still
    // come back to ₹20.00 once LineTotal rounds to paise.
    private static decimal PerUnit(decimal packPrice, ProductResponse product)
        => Math.Round(packPrice / (product.UnitsPerPack > 0 ? product.UnitsPerPack : 1m), 4, MidpointRounding.AwayFromZero);
}
