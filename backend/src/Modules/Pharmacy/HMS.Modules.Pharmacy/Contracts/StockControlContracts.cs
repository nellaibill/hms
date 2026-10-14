using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Contracts;

public enum AdjustmentReason
{
    /// <summary>Expired stock destroyed rather than returned to a supplier.</summary>
    Expired,
    Damaged,

    /// <summary>The shelf holds less than the books and nobody knows why.</summary>
    Shortage,

    /// <summary>The shelf holds more than the books.</summary>
    Excess,

    /// <summary>Given away, or used as a sample.</summary>
    FreeIssue,

    /// <summary>The differences found by a stock count.</summary>
    StockCount,

    /// <summary>Putting right an earlier entry made in error.</summary>
    Correction,
    Other,
}

public enum StockCountStatus
{
    Open,
    Posted,
    Cancelled,
}

// ---- Adjustments -----------------------------------------------------------------------

public record StockAdjustmentLineRequest
{
    public Guid ProductId { get; init; }
    public Guid ProductBatchId { get; init; }

    /// <summary>Units, signed: positive adds to stock, negative takes away.</summary>
    public decimal Quantity { get; init; }
}

public record CreateStockAdjustmentRequest
{
    public Guid? StoreId { get; init; }
    public AdjustmentReason Reason { get; init; }
    public string? Remarks { get; init; }
    public IReadOnlyList<StockAdjustmentLineRequest> Lines { get; init; } = [];
}

public record StockAdjustmentLineResponse
{
    public Guid Id { get; init; }
    public int LineNo { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public decimal Quantity { get; init; }

    /// <summary>Quantity × unit cost. Null for a caller who may not see cost, or when no cost is known.</summary>
    public decimal? Value { get; init; }
}

public record StockAdjustmentResponse
{
    public Guid Id { get; init; }
    public string AdjustmentNo { get; init; } = string.Empty;
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public DateOnly AdjustmentDate { get; init; }
    public AdjustmentReason Reason { get; init; }
    public Guid? StockCountId { get; init; }
    public string? Remarks { get; init; }
    public int LineCount { get; init; }

    /// <summary>Net value of the adjustment at cost; negative is a loss. Null without pharmacy.view-cost.</summary>
    public decimal? TotalValue { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<StockAdjustmentLineResponse> Lines { get; init; } = [];
}

public class StockAdjustmentListQuery : PagedRequest
{
    public Guid? StoreId { get; set; }
    public AdjustmentReason? Reason { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

// ---- Transfers -------------------------------------------------------------------------

public record StockTransferLineRequest
{
    public Guid ProductId { get; init; }
    public Guid ProductBatchId { get; init; }

    /// <summary>Units.</summary>
    public decimal Quantity { get; init; }
}

public record CreateStockTransferRequest
{
    public Guid FromStoreId { get; init; }
    public Guid ToStoreId { get; init; }
    public string? Remarks { get; init; }
    public IReadOnlyList<StockTransferLineRequest> Lines { get; init; } = [];
}

public record StockTransferLineResponse
{
    public Guid Id { get; init; }
    public int LineNo { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public decimal Quantity { get; init; }
}

public record StockTransferResponse
{
    public Guid Id { get; init; }
    public string TransferNo { get; init; } = string.Empty;
    public Guid FromStoreId { get; init; }
    public string FromStoreName { get; init; } = string.Empty;
    public Guid ToStoreId { get; init; }
    public string ToStoreName { get; init; } = string.Empty;
    public DateOnly TransferDate { get; init; }
    public string? Remarks { get; init; }
    public int LineCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<StockTransferLineResponse> Lines { get; init; } = [];
}

public class StockTransferListQuery : PagedRequest
{
    public Guid? StoreId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

// ---- Stock count -----------------------------------------------------------------------

public record StartStockCountRequest
{
    public Guid? StoreId { get; init; }
    public string? Remarks { get; init; }
}

public record StockCountEntry
{
    public Guid LineId { get; init; }

    /// <summary>Units found on the shelf. Null clears the line back to "not counted".</summary>
    public decimal? CountedQuantity { get; init; }
}

public record SaveStockCountRequest
{
    public IReadOnlyList<StockCountEntry> Entries { get; init; } = [];
}

public record StockCountLineResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? RackLocation { get; init; }
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }

    /// <summary>What the books held when the count started, and what they hold now. They
    /// differ when stock moved while the count was open.</summary>
    public decimal BookQuantity { get; init; }
    public decimal CurrentQuantity { get; init; }
    public decimal? CountedQuantity { get; init; }

    /// <summary>Counted less current book. Null until the line is counted.</summary>
    public decimal? Variance { get; init; }
}

public record StockCountResponse
{
    public Guid Id { get; init; }
    public string CountNo { get; init; } = string.Empty;
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public StockCountStatus Status { get; init; }
    public DateTime? PostedAt { get; init; }
    public Guid? AdjustmentId { get; init; }
    public string? Remarks { get; init; }
    public int LineCount { get; init; }
    public int CountedLines { get; init; }

    /// <summary>Counted lines whose count differs from the book.</summary>
    public int VarianceLines { get; init; }

    /// <summary>Empty on list responses; filled when a single count is fetched.</summary>
    public IReadOnlyList<StockCountLineResponse> Lines { get; init; } = [];
}

public class StockCountListQuery : PagedRequest
{
    public Guid? StoreId { get; set; }
    public StockCountStatus? Status { get; set; }
}

// ---- Expiry ----------------------------------------------------------------------------

/// <summary>A batch on the shelf that has expired or will within the window.</summary>
public record ExpiringBatchResponse
{
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? RackLocation { get; init; }
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }

    /// <summary>Days until expiry; negative once it has passed.</summary>
    public int DaysLeft { get; init; }
    public decimal QuantityOnHand { get; init; }

    /// <summary>At cost. Null for a caller who may not see cost, or when no cost is known.</summary>
    public decimal? Value { get; init; }

    /// <summary>Who the batch was bought from, when it came in on a purchase — who to return it to.</summary>
    public Guid? SupplierId { get; init; }
    public string? SupplierName { get; init; }
}

// ---- Statutory registers ---------------------------------------------------------------

/// <summary>One sale of a scheduled drug, as the register requires it to be recorded.</summary>
public record ScheduleRegisterEntry
{
    public DateTime SaleDate { get; init; }
    public Guid SaleId { get; init; }
    public string BillNo { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string? PatientUhid { get; init; }
    public string? PatientPhone { get; init; }
    public string? DoctorName { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public DrugSchedule Schedule { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public decimal Quantity { get; init; }
    public decimal ReturnedQuantity { get; init; }
}
