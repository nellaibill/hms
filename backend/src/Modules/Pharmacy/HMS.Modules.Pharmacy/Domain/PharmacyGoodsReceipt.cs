using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Domain;

/// <summary>
/// A supplier's invoice as received into a store — the document purchases come in on. Once
/// saved it is posted: its lines have already moved stock, so it is never edited. A mistake
/// is put right with a purchase return or a stock adjustment, which leave their own trail.
/// Quantities on the lines are in packs, as the supplier bills them; stock moves in units
/// (packs × units per pack).
/// </summary>
internal class PharmacyGoodsReceipt : Entity
{
    private readonly List<PharmacyGoodsReceiptLine> _lines = [];

    public string ReceiptNo { get; private set; } = null!;
    public Guid StoreId { get; private set; }
    public Guid SupplierId { get; private set; }

    /// <summary>The supplier's name as it stood on the day — the document must still read
    /// correctly after the supplier master is edited.</summary>
    public string SupplierName { get; private set; } = null!;
    public string SupplierInvoiceNo { get; private set; } = null!;
    public DateOnly? SupplierInvoiceDate { get; private set; }
    public DateOnly ReceivedDate { get; private set; }

    /// <summary>Supplier is in another state, so the tax on this invoice is IGST rather than
    /// CGST + SGST.</summary>
    public bool IsInterState { get; private set; }
    public Guid? PurchaseOrderId { get; private set; }

    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxableAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal OtherCharges { get; private set; }
    public decimal RoundOff { get; private set; }
    public decimal NetAmount { get; private set; }
    public string? Remarks { get; private set; }

    public IReadOnlyList<PharmacyGoodsReceiptLine> Lines => _lines;

    // Required by EF Core materialization.
    private PharmacyGoodsReceipt()
    {
    }

    private PharmacyGoodsReceipt(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacyGoodsReceipt Create(
        string receiptNo,
        Guid storeId,
        Guid supplierId,
        string supplierName,
        string supplierInvoiceNo,
        DateOnly? supplierInvoiceDate,
        DateOnly receivedDate,
        bool isInterState,
        Guid? purchaseOrderId,
        decimal otherCharges,
        string? remarks,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(receiptNo, nameof(receiptNo));
        Guard.AgainstNullOrWhiteSpace(supplierName, nameof(supplierName));
        Guard.AgainstNullOrWhiteSpace(supplierInvoiceNo, nameof(supplierInvoiceNo));
        if (otherCharges < 0) throw new ArgumentOutOfRangeException(nameof(otherCharges), "Other charges cannot be negative.");

        return new PharmacyGoodsReceipt(Guid.CreateVersion7(), createdBy)
        {
            ReceiptNo = receiptNo,
            StoreId = storeId,
            SupplierId = supplierId,
            SupplierName = supplierName.Trim(),
            SupplierInvoiceNo = supplierInvoiceNo.Trim(),
            SupplierInvoiceDate = supplierInvoiceDate,
            ReceivedDate = receivedDate,
            IsInterState = isInterState,
            PurchaseOrderId = purchaseOrderId,
            OtherCharges = otherCharges,
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
        };
    }

    public PharmacyGoodsReceiptLine AddLine(
        Guid productId,
        string productName,
        string? hsnCode,
        Guid productBatchId,
        string batchNo,
        DateOnly expiryDate,
        decimal unitsPerPack,
        decimal quantity,
        decimal freeQuantity,
        decimal purchaseRate,
        decimal mrp,
        decimal? sellingRate,
        decimal discountPercent,
        decimal gstRatePercent,
        decimal gross,
        decimal discount,
        decimal taxable,
        decimal tax)
    {
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        if (freeQuantity < 0) throw new ArgumentOutOfRangeException(nameof(freeQuantity), "Free quantity cannot be negative.");
        if (quantity + freeQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "A line must receive something.");
        Guard.AgainstNonPositive(unitsPerPack, nameof(unitsPerPack));

        var line = new PharmacyGoodsReceiptLine(
            Id, _lines.Count + 1, productId, productName, hsnCode, productBatchId, batchNo, expiryDate, unitsPerPack, quantity, freeQuantity,
            purchaseRate, mrp, sellingRate, discountPercent, gstRatePercent, gross, discount, taxable, tax, CreatedBy);

        _lines.Add(line);
        GrossAmount += gross;
        DiscountAmount += discount;
        TaxableAmount += taxable;
        TaxAmount += tax;
        return line;
    }

    /// <summary>Call once every line is on: settles the invoice total in whole rupees.</summary>
    public void Close()
    {
        var total = TaxableAmount + TaxAmount + OtherCharges;
        NetAmount = Math.Round(total, 0, MidpointRounding.AwayFromZero);
        RoundOff = NetAmount - total;
    }
}

internal class PharmacyGoodsReceiptLine : Entity
{
    public Guid GoodsReceiptId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public string? HsnCode { get; private set; }
    public Guid ProductBatchId { get; private set; }
    public string BatchNo { get; private set; } = null!;
    public DateOnly ExpiryDate { get; private set; }
    public decimal UnitsPerPack { get; private set; }

    /// <summary>Packs billed, and packs given free on top (a "10 + 1" scheme).</summary>
    public decimal Quantity { get; private set; }
    public decimal FreeQuantity { get; private set; }

    /// <summary>Per pack, before tax.</summary>
    public decimal PurchaseRate { get; private set; }
    public decimal Mrp { get; private set; }
    public decimal? SellingRate { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal GstRatePercent { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxableAmount { get; private set; }
    public decimal TaxAmount { get; private set; }

    /// <summary>Units since sent back to the supplier on purchase returns.</summary>
    public decimal ReturnedUnits { get; private set; }

    public decimal LineTotal => TaxableAmount + TaxAmount;

    /// <summary>Units this line put into stock: billed and free packs alike.</summary>
    public decimal ReceivedUnits => (Quantity + FreeQuantity) * UnitsPerPack;

    /// <summary>What one unit really cost: the taxable amount spread over every unit that
    /// arrived, so free goods and discounts lower it. Tax is left out — it is not a cost of
    /// the goods.</summary>
    public decimal UnitCost => ReceivedUnits > 0 ? Math.Round(TaxableAmount / ReceivedUnits, 4, MidpointRounding.AwayFromZero) : 0m;

    // Required by EF Core materialization.
    private PharmacyGoodsReceiptLine()
    {
    }

    internal PharmacyGoodsReceiptLine(
        Guid goodsReceiptId, int lineNo, Guid productId, string productName, string? hsnCode, Guid productBatchId, string batchNo,
        DateOnly expiryDate, decimal unitsPerPack, decimal quantity, decimal freeQuantity, decimal purchaseRate, decimal mrp,
        decimal? sellingRate, decimal discountPercent, decimal gstRatePercent, decimal gross, decimal discount, decimal taxable,
        decimal tax, Guid? createdBy)
        : base(Guid.CreateVersion7(), createdBy)
    {
        GoodsReceiptId = goodsReceiptId;
        LineNo = lineNo;
        ProductId = productId;
        ProductName = productName;
        HsnCode = hsnCode;
        ProductBatchId = productBatchId;
        BatchNo = batchNo;
        ExpiryDate = expiryDate;
        UnitsPerPack = unitsPerPack;
        Quantity = quantity;
        FreeQuantity = freeQuantity;
        PurchaseRate = purchaseRate;
        Mrp = mrp;
        SellingRate = sellingRate;
        DiscountPercent = discountPercent;
        GstRatePercent = gstRatePercent;
        GrossAmount = gross;
        DiscountAmount = discount;
        TaxableAmount = taxable;
        TaxAmount = tax;
    }

    public void RecordReturn(decimal units, Guid? updatedBy)
    {
        Guard.AgainstNonPositive(units, nameof(units));
        if (ReturnedUnits + units > ReceivedUnits)
        {
            throw new InvalidOperationException($"Only {ReceivedUnits - ReturnedUnits:0.####} of this line is left to return.");
        }

        ReturnedUnits += units;
        MarkUpdated(updatedBy);
    }
}
