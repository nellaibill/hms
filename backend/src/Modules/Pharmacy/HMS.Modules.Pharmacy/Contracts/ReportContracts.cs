namespace HMS.Modules.Pharmacy.Contracts;

// Every figure a report shows is summed from the documents themselves — bills, returns, goods
// receipts, the stock balances. Nothing here is stored. Cost, margin and stock value are null
// for a caller without pharmacy.view-cost.

// ---- Sales -----------------------------------------------------------------------------

/// <summary>One hospital-local day of counter business.</summary>
public record DailySalesRow
{
    public DateOnly Date { get; init; }
    public int Bills { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }

    /// <summary>Bill value of the day's bills, in whole rupees as billed.</summary>
    public decimal SalesAmount { get; init; }
    public int Returns { get; init; }

    /// <summary>Bill value of goods brought back on the day, whichever day they were sold.</summary>
    public decimal ReturnAmount { get; init; }
    public decimal NetSales { get; init; }

    /// <summary>Money taken at the counter on the day less money refunded — not the same as
    /// <see cref="NetSales"/>: credit bills are sold today and paid later.</summary>
    public decimal Collected { get; init; }

    /// <summary>Cost of what was sold less cost of what came back.</summary>
    public decimal? Cost { get; init; }

    /// <summary>Taxable value net of returns, less <see cref="Cost"/>.</summary>
    public decimal? Margin { get; init; }
}

public record SalesRegisterRow
{
    public Guid SaleId { get; init; }
    public string BillNo { get; init; } = string.Empty;
    public DateTime SaleDate { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? PatientUhid { get; init; }

    /// <summary>"IP" for a bill against an admission, "OP" for a registered patient, else "Walk-in".</summary>
    public string Kind { get; init; } = string.Empty;
    public string? DoctorName { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal RoundOff { get; init; }
    public decimal NetAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal ReturnedAmount { get; init; }
    public decimal BalanceDue { get; init; }
    public bool ChargedToAdmission { get; init; }
}

public record SalesReturnRegisterRow
{
    public Guid ReturnId { get; init; }
    public string ReturnNo { get; init; } = string.Empty;
    public Guid SaleId { get; init; }
    public string BillNo { get; init; } = string.Empty;
    public DateTime ReturnDate { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ReturnAmount { get; init; }
    public decimal RefundAmount { get; init; }
    public SalePaymentMethod RefundMethod { get; init; }
}

public record SalesRegisterResponse
{
    public IReadOnlyList<SalesRegisterRow> Bills { get; init; } = [];
    public IReadOnlyList<SalesReturnRegisterRow> Returns { get; init; } = [];
}

/// <summary>What one product sold in a period, net of what came back.</summary>
public record ProductSalesRow
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Bills { get; init; }
    public decimal QuantitySold { get; init; }
    public decimal QuantityReturned { get; init; }
    public decimal NetQuantity { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal Amount { get; init; }
    public decimal? Cost { get; init; }
    public decimal? Margin { get; init; }

    /// <summary>Margin as a share of taxable value.</summary>
    public decimal? MarginPercent { get; init; }
}

/// <summary>Money through the counter on a day by one payment mode.</summary>
public record CollectionRow
{
    public DateOnly Date { get; init; }
    public SalePaymentMethod Method { get; init; }
    public decimal Received { get; init; }
    public decimal Refunded { get; init; }
    public decimal Net { get; init; }
}

// ---- GST -------------------------------------------------------------------------------

/// <summary>Taxable value and tax at one HSN code and rate.</summary>
public record GstRateRow
{
    public string? HsnCode { get; init; }
    public decimal GstRatePercent { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal Cgst { get; init; }
    public decimal Sgst { get; init; }
    public decimal Igst { get; init; }
    public decimal TaxAmount { get; init; }
}

/// <summary>GST on what was sold and what was bought in a period. Counter sales are within
/// the state (CGST + SGST); a purchase follows its invoice, which may be IGST.</summary>
public record GstSummaryResponse
{
    public IReadOnlyList<GstRateRow> Sales { get; init; } = [];
    public IReadOnlyList<GstRateRow> SaleReturns { get; init; } = [];
    public IReadOnlyList<GstRateRow> Purchases { get; init; } = [];
    public IReadOnlyList<GstRateRow> PurchaseReturns { get; init; } = [];

    /// <summary>Tax on sales less tax on sale returns.</summary>
    public decimal OutputTax { get; init; }

    /// <summary>Tax on purchases less tax on purchase returns. Null, with the purchase tables
    /// empty, for a caller who may not see what the pharmacy pays.</summary>
    public decimal? InputTax { get; init; }
}

// ---- Purchases -------------------------------------------------------------------------

public record PurchaseRegisterRow
{
    public Guid ReceiptId { get; init; }
    public string ReceiptNo { get; init; } = string.Empty;
    public DateOnly ReceivedDate { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string SupplierInvoiceNo { get; init; } = string.Empty;
    public DateOnly? SupplierInvoiceDate { get; init; }
    public bool IsInterState { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal OtherCharges { get; init; }
    public decimal RoundOff { get; init; }
    public decimal NetAmount { get; init; }
}

public record PurchaseReturnRegisterRow
{
    public Guid ReturnId { get; init; }
    public string ReturnNo { get; init; } = string.Empty;
    public DateOnly ReturnDate { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public PurchaseReturnReason Reason { get; init; }
    public string? CreditNoteNo { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal NetAmount { get; init; }
}

public record PurchaseRegisterResponse
{
    public IReadOnlyList<PurchaseRegisterRow> Purchases { get; init; } = [];
    public IReadOnlyList<PurchaseReturnRegisterRow> Returns { get; init; } = [];
}

// ---- Stock -----------------------------------------------------------------------------

/// <summary>What a product's stock on hand is worth.</summary>
public record StockValuationRow
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public string? RackLocation { get; init; }
    public int Batches { get; init; }
    public decimal Quantity { get; init; }

    /// <summary>At each batch's purchase rate. Null when no batch has a known cost.</summary>
    public decimal? CostValue { get; init; }
    public decimal MrpValue { get; init; }

    /// <summary>The part of <see cref="Quantity"/> past its expiry date, and its cost.</summary>
    public decimal ExpiredQuantity { get; init; }
    public decimal? ExpiredCostValue { get; init; }
}

public record StockValuationResponse
{
    public IReadOnlyList<StockValuationRow> Rows { get; init; } = [];
    public decimal TotalQuantity { get; init; }
    public decimal? TotalCostValue { get; init; }
    public decimal TotalMrpValue { get; init; }
    public decimal? ExpiredCostValue { get; init; }
}

/// <summary>Stock on the shelf that has not sold for a while, or ever.</summary>
public record NonMovingStockRow
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? RackLocation { get; init; }
    public decimal Quantity { get; init; }
    public decimal? CostValue { get; init; }

    /// <summary>Null when the product has never been billed.</summary>
    public DateOnly? LastSoldOn { get; init; }

    /// <summary>When the oldest batch still on the shelf was first recorded.</summary>
    public DateOnly? StockedOn { get; init; }

    /// <summary>Days since the last sale — or, never sold, since it was stocked.</summary>
    public int IdleDays { get; init; }
    public DateOnly? NearestExpiry { get; init; }
}

// ---- Dashboard -------------------------------------------------------------------------

public record SalesFigures
{
    public int Bills { get; init; }
    public decimal SalesAmount { get; init; }
    public decimal ReturnAmount { get; init; }
    public decimal NetSales { get; init; }
    public decimal Collected { get; init; }
    public decimal? Margin { get; init; }
}

public record PaymentModeTotal
{
    public SalePaymentMethod Method { get; init; }
    public decimal Amount { get; init; }
}

public record StockAlertFigures
{
    public int ProductsInStock { get; init; }
    public int LowStock { get; init; }
    public int OutOfStock { get; init; }
    public int ExpiredBatches { get; init; }
    public int ExpiringSoonBatches { get; init; }
    public int ExpiryAlertDays { get; init; }
    public decimal? StockValue { get; init; }
    public decimal? ExpiredValue { get; init; }
}

public record PendingWorkFigures
{
    public int PendingIndents { get; init; }
    public int OpenPurchaseOrders { get; init; }
    public int BillsWithDues { get; init; }
    public decimal CustomerDues { get; init; }

    /// <summary>What suppliers are owed in all. Null without pharmacy.view-cost.</summary>
    public decimal? SupplierPayable { get; init; }
}

/// <summary>The pharmacy at a glance: today, the month so far, the last fortnight day by day,
/// what needs attention on the shelf, and what is waiting to be done.</summary>
public record PharmacyDashboardResponse
{
    public DateOnly Date { get; init; }
    public SalesFigures Today { get; init; } = new();
    public SalesFigures Yesterday { get; init; } = new();
    public SalesFigures MonthToDate { get; init; } = new();
    public IReadOnlyList<DailySalesRow> Trend { get; init; } = [];
    public IReadOnlyList<PaymentModeTotal> TodayCollections { get; init; } = [];
    public StockAlertFigures Stock { get; init; } = new();
    public PendingWorkFigures Pending { get; init; } = new();

    /// <summary>Best sellers by value over the last 30 days.</summary>
    public IReadOnlyList<ProductSalesRow> TopProducts { get; init; } = [];
}
