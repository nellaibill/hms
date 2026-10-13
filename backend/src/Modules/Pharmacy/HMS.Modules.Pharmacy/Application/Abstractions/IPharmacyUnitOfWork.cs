using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application.Abstractions;

/// <summary>A printed-number series: one database sequence and the prefix its numbers carry
/// ("GRN-2026-000042").</summary>
internal sealed record DocumentSeries(string SequenceName, string Prefix)
{
    public static readonly DocumentSeries GoodsReceipt = new("goods_receipt_no_seq", "GRN");
    public static readonly DocumentSeries Sale = new("sale_no_seq", "PH");
    public static readonly DocumentSeries SaleReturn = new("sale_return_no_seq", "SR");
    public static readonly DocumentSeries PurchaseOrder = new("purchase_order_no_seq", "PO");
    public static readonly DocumentSeries PurchaseReturn = new("purchase_return_no_seq", "PR");
    public static readonly DocumentSeries SupplierPayment = new("supplier_payment_no_seq", "SP");
    public static readonly DocumentSeries Transfer = new("transfer_no_seq", "ST");
    public static readonly DocumentSeries Adjustment = new("adjustment_no_seq", "ADJ");
    public static readonly DocumentSeries StockCount = new("stock_count_no_seq", "SC");
    public static readonly DocumentSeries Indent = new("indent_no_seq", "IND");

    public static readonly IReadOnlyList<DocumentSeries> All =
        [GoodsReceipt, Sale, SaleReturn, PurchaseOrder, PurchaseReturn, SupplierPayment, Transfer, Adjustment, StockCount, Indent];
}

/// <summary>
/// The one place a pharmacy document is committed. Every repository in this module shares
/// the request's PharmacyDbContext, so a document, its lines, the balances it moved and the
/// ledger rows it wrote all go in with a single save — or none of them do.
/// </summary>
internal interface IPharmacyUnitOfWork
{
    /// <summary>Next number in a series. Taken from a database sequence, so two counters
    /// billing at once never get the same one.</summary>
    Task<string> NextNumberAsync(DocumentSeries series, CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="work"/> and saves. If another request changed one of the same
    /// stock balances first, everything tracked is thrown away and the work is run again
    /// against fresh quantities — so a second counter selling the last strip gets "only 0
    /// available", not a negative balance or a raw database error. <paramref name="work"/>
    /// must therefore build everything it adds from scratch each time it is called.
    /// </summary>
    Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> work, CancellationToken cancellationToken);
}
