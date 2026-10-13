using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Contracts;

public record GoodsReceiptLineRequest
{
    public Guid ProductId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public DateOnly? ManufactureDate { get; init; }

    /// <summary>Packs billed, and packs free on top.</summary>
    public decimal Quantity { get; init; }
    public decimal FreeQuantity { get; init; }

    /// <summary>Per pack. PurchaseRate is before tax; Mrp includes it.</summary>
    public decimal PurchaseRate { get; init; }
    public decimal Mrp { get; init; }

    /// <summary>What the pharmacy sells a pack at, if below MRP. Omitted, it sells at MRP.</summary>
    public decimal? SellingRate { get; init; }
    public decimal DiscountPercent { get; init; }

    /// <summary>Omitted, the product's own GST rate applies (or 0 if it has none).</summary>
    public decimal? GstRatePercent { get; init; }
}

public record CreateGoodsReceiptRequest
{
    /// <summary>The store receiving the goods. Omitted, it is the default store.</summary>
    public Guid? StoreId { get; init; }
    public Guid SupplierId { get; init; }
    public string SupplierInvoiceNo { get; init; } = string.Empty;
    public DateOnly? SupplierInvoiceDate { get; init; }

    /// <summary>Omitted, today.</summary>
    public DateOnly? ReceivedDate { get; init; }
    public bool IsInterState { get; init; }
    public Guid? PurchaseOrderId { get; init; }

    /// <summary>Freight, packing and the like — added to the invoice total, not to stock cost.</summary>
    public decimal OtherCharges { get; init; }
    public string? Remarks { get; init; }
    public IReadOnlyList<GoodsReceiptLineRequest> Lines { get; init; } = [];
}

public record GoodsReceiptLineResponse
{
    public Guid Id { get; init; }
    public int LineNo { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? HsnCode { get; init; }
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public decimal UnitsPerPack { get; init; }
    public decimal Quantity { get; init; }
    public decimal FreeQuantity { get; init; }
    public decimal PurchaseRate { get; init; }
    public decimal Mrp { get; init; }
    public decimal? SellingRate { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal GstRatePercent { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal LineTotal { get; init; }
    public decimal ReceivedUnits { get; init; }
    public decimal ReturnedUnits { get; init; }
}

public record GoodsReceiptResponse
{
    public Guid Id { get; init; }
    public string ReceiptNo { get; init; } = string.Empty;
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string SupplierInvoiceNo { get; init; } = string.Empty;
    public DateOnly? SupplierInvoiceDate { get; init; }
    public DateOnly ReceivedDate { get; init; }
    public bool IsInterState { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal OtherCharges { get; init; }
    public decimal RoundOff { get; init; }
    public decimal NetAmount { get; init; }
    public string? Remarks { get; init; }
    public int LineCount { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Empty on list responses; filled when a single receipt is fetched.</summary>
    public IReadOnlyList<GoodsReceiptLineResponse> Lines { get; init; } = [];
}

public class GoodsReceiptListQuery : PagedRequest
{
    public Guid? SupplierId { get; set; }
    public Guid? StoreId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
