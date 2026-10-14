using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Contracts;

public enum PurchaseOrderStatus
{
    /// <summary>Being put together; can still be edited.</summary>
    Draft,

    /// <summary>Sent to the supplier; nothing received yet.</summary>
    Ordered,
    PartlyReceived,
    Received,
    Cancelled,
}

public enum PurchaseReturnReason
{
    Expired,
    NearExpiry,
    Damaged,
    Excess,
    WrongItem,
    RateDifference,
    Other,
}

public enum SupplierPaymentMethod
{
    Cash,
    Cheque,
    BankTransfer,
    Upi,
}

// ---- Purchase orders -------------------------------------------------------------------

public record PurchaseOrderLineRequest
{
    public Guid ProductId { get; init; }

    /// <summary>Packs.</summary>
    public decimal Quantity { get; init; }

    /// <summary>Per pack, before tax. Omitted, the product's last cost is used.</summary>
    public decimal? ExpectedRate { get; init; }
}

public record SavePurchaseOrderRequest
{
    public Guid? StoreId { get; init; }
    public Guid SupplierId { get; init; }
    public DateOnly? ExpectedDate { get; init; }
    public string? Remarks { get; init; }
    public IReadOnlyList<PurchaseOrderLineRequest> Lines { get; init; } = [];

    /// <summary>Send it to the supplier straight away rather than leaving it a draft.</summary>
    public bool PlaceOrder { get; init; }
}

public record PurchaseOrderLineResponse
{
    public Guid Id { get; init; }
    public int LineNo { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal ReceivedQuantity { get; init; }
    public decimal PendingQuantity { get; init; }
    public decimal ExpectedRate { get; init; }
    public decimal GstRatePercent { get; init; }
}

public record PurchaseOrderResponse
{
    public Guid Id { get; init; }
    public string OrderNo { get; init; } = string.Empty;
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public DateOnly OrderDate { get; init; }
    public DateOnly? ExpectedDate { get; init; }
    public PurchaseOrderStatus Status { get; init; }
    public decimal EstimatedAmount { get; init; }
    public string? Remarks { get; init; }
    public int LineCount { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Empty on list responses; filled when a single order is fetched.</summary>
    public IReadOnlyList<PurchaseOrderLineResponse> Lines { get; init; } = [];
}

public class PurchaseOrderListQuery : PagedRequest
{
    public Guid? SupplierId { get; set; }
    public PurchaseOrderStatus? Status { get; set; }

    /// <summary>Only orders goods can still be received against.</summary>
    public bool? OpenOnly { get; set; }
}

/// <summary>
/// A product that needs buying: what the store has, what is already on order, and a suggested
/// quantity in packs, with where it was last bought and at what rate.
/// </summary>
public record ReorderSuggestionResponse
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public decimal UnitsPerPack { get; init; }
    public decimal AvailableQuantity { get; init; }
    public decimal ReorderLevel { get; init; }
    public StockStatus Status { get; init; }

    /// <summary>Packs on open purchase orders and not yet received.</summary>
    public decimal OnOrderQuantity { get; init; }

    /// <summary>Packs to order now. Zero when what is already on order covers the need.</summary>
    public decimal SuggestedQuantity { get; init; }
    public Guid? LastSupplierId { get; init; }
    public string? LastSupplierName { get; init; }

    /// <summary>Per pack. Null for a caller who may not see cost.</summary>
    public decimal? LastPurchaseRate { get; init; }
    public decimal GstRatePercent { get; init; }
}

// ---- Purchase returns ------------------------------------------------------------------

public record PurchaseReturnLineRequest
{
    public Guid ProductId { get; init; }
    public Guid ProductBatchId { get; init; }

    /// <summary>Units.</summary>
    public decimal Quantity { get; init; }

    /// <summary>What the supplier credits per unit, before tax. Omitted, the batch's own
    /// purchase rate is used.</summary>
    public decimal? UnitRate { get; init; }
}

public record CreatePurchaseReturnRequest
{
    public Guid? StoreId { get; init; }
    public Guid SupplierId { get; init; }
    public DateOnly? ReturnDate { get; init; }
    public PurchaseReturnReason Reason { get; init; }
    public string? CreditNoteNo { get; init; }
    public string? Remarks { get; init; }
    public IReadOnlyList<PurchaseReturnLineRequest> Lines { get; init; } = [];
}

public record PurchaseReturnLineResponse
{
    public Guid Id { get; init; }
    public int LineNo { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitRate { get; init; }
    public decimal GstRatePercent { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal LineTotal { get; init; }
}

public record PurchaseReturnResponse
{
    public Guid Id { get; init; }
    public string ReturnNo { get; init; } = string.Empty;
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public DateOnly ReturnDate { get; init; }
    public PurchaseReturnReason Reason { get; init; }
    public string? CreditNoteNo { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal RoundOff { get; init; }
    public decimal NetAmount { get; init; }
    public string? Remarks { get; init; }
    public int LineCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<PurchaseReturnLineResponse> Lines { get; init; } = [];
}

public class PurchaseReturnListQuery : PagedRequest
{
    public Guid? SupplierId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

/// <summary>A batch the store holds that can be sent back — any batch with stock, expired or not.</summary>
public record ReturnableBatchResponse
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public decimal QuantityOnHand { get; init; }
    public decimal UnitsPerPack { get; init; }

    /// <summary>Per unit, before tax.</summary>
    public decimal? UnitRate { get; init; }
    public decimal GstRatePercent { get; init; }
}

// ---- Supplier account ------------------------------------------------------------------

public record CreateSupplierPaymentRequest
{
    public DateOnly? PaymentDate { get; init; }
    public decimal Amount { get; init; }
    public SupplierPaymentMethod Method { get; init; }
    public string? ReferenceNo { get; init; }
    public string? Remarks { get; init; }
}

public record SupplierPaymentResponse
{
    public Guid Id { get; init; }
    public string PaymentNo { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public DateOnly PaymentDate { get; init; }
    public decimal Amount { get; init; }
    public SupplierPaymentMethod Method { get; init; }
    public string? ReferenceNo { get; init; }
    public string? Remarks { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Where a supplier's account stands: bought, sent back, paid, still owed.</summary>
public record SupplierBalanceResponse
{
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public decimal Purchases { get; init; }
    public decimal Returns { get; init; }
    public decimal Payments { get; init; }

    /// <summary>Purchases less returns less payments. Negative means the supplier owes the pharmacy.</summary>
    public decimal Outstanding { get; init; }
    public DateOnly? LastPurchaseDate { get; init; }
}

public enum SupplierEntryType
{
    Purchase,
    Return,
    Payment,
}

/// <summary>One line of a supplier's statement.</summary>
public record SupplierStatementEntry
{
    public DateOnly Date { get; init; }
    public SupplierEntryType Type { get; init; }
    public Guid DocumentId { get; init; }
    public string DocumentNo { get; init; } = string.Empty;

    /// <summary>The supplier's own invoice or credit-note number, or the payment reference.</summary>
    public string? Reference { get; init; }

    /// <summary>Adds to what is owed (a purchase).</summary>
    public decimal Credit { get; init; }

    /// <summary>Takes off what is owed (a return, a payment).</summary>
    public decimal Debit { get; init; }
    public decimal Balance { get; init; }
}

public record SupplierStatementResponse
{
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }

    /// <summary>What was owed before the first entry shown.</summary>
    public decimal OpeningBalance { get; init; }
    public decimal ClosingBalance { get; init; }
    public IReadOnlyList<SupplierStatementEntry> Entries { get; init; } = [];
}
