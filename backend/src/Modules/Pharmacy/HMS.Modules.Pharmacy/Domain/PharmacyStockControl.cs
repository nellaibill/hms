using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Domain;

/// <summary>
/// A correction to what the books say a store holds — breakage, an expired batch written
/// off, a shortage found on the shelf, stock found that the books did not have. It is the
/// only way a balance changes without a purchase, a sale or a transfer behind it, so it
/// always carries a reason and is always its own numbered document.
/// </summary>
internal class PharmacyStockAdjustment : Entity
{
    private readonly List<PharmacyStockAdjustmentLine> _lines = [];

    public string AdjustmentNo { get; private set; } = null!;
    public Guid StoreId { get; private set; }
    public DateOnly AdjustmentDate { get; private set; }
    public AdjustmentReason Reason { get; private set; }

    /// <summary>The stock count this adjustment posted the variances of, when it came from one.</summary>
    public Guid? StockCountId { get; private set; }
    public string? Remarks { get; private set; }

    public IReadOnlyList<PharmacyStockAdjustmentLine> Lines => _lines;

    // Required by EF Core materialization.
    private PharmacyStockAdjustment()
    {
    }

    private PharmacyStockAdjustment(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacyStockAdjustment Create(string adjustmentNo, Guid storeId, DateOnly adjustmentDate, AdjustmentReason reason, Guid? stockCountId, string? remarks, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(adjustmentNo, nameof(adjustmentNo));

        return new PharmacyStockAdjustment(Guid.CreateVersion7(), createdBy)
        {
            AdjustmentNo = adjustmentNo,
            StoreId = storeId,
            AdjustmentDate = adjustmentDate,
            Reason = reason,
            StockCountId = stockCountId,
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
        };
    }

    /// <param name="quantity">Signed, in units: positive adds to stock, negative takes away.</param>
    public PharmacyStockAdjustmentLine AddLine(Guid productId, string productName, Guid productBatchId, string batchNo, DateOnly expiryDate, decimal quantity, decimal? unitCost)
    {
        if (quantity == 0) throw new ArgumentOutOfRangeException(nameof(quantity), "An adjustment line must change the quantity.");

        var line = new PharmacyStockAdjustmentLine(Id, _lines.Count + 1, productId, productName, productBatchId, batchNo, expiryDate, quantity, unitCost, CreatedBy);
        _lines.Add(line);
        return line;
    }
}

internal class PharmacyStockAdjustmentLine : Entity
{
    public Guid AdjustmentId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public Guid ProductBatchId { get; private set; }
    public string BatchNo { get; private set; } = null!;
    public DateOnly ExpiryDate { get; private set; }

    /// <summary>Signed, in units.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>What a unit cost when the adjustment was made — what the loss or gain is valued at.</summary>
    public decimal? UnitCost { get; private set; }

    // Required by EF Core materialization.
    private PharmacyStockAdjustmentLine()
    {
    }

    internal PharmacyStockAdjustmentLine(
        Guid adjustmentId, int lineNo, Guid productId, string productName, Guid productBatchId, string batchNo, DateOnly expiryDate, decimal quantity, decimal? unitCost, Guid? createdBy)
        : base(Guid.CreateVersion7(), createdBy)
    {
        AdjustmentId = adjustmentId;
        LineNo = lineNo;
        ProductId = productId;
        ProductName = productName;
        ProductBatchId = productBatchId;
        BatchNo = batchNo;
        ExpiryDate = expiryDate;
        Quantity = quantity;
        UnitCost = unitCost;
    }
}

/// <summary>
/// Stock moved from one store to another — main store to a counter, a counter to a ward. It
/// is posted in one step: the stock leaves the first store and is in the second the moment
/// the transfer is saved. The two stores are in one building; a "goods in transit" state
/// would only be a second thing to forget to complete.
/// </summary>
internal class PharmacyStockTransfer : Entity
{
    private readonly List<PharmacyStockTransferLine> _lines = [];

    public string TransferNo { get; private set; } = null!;
    public Guid FromStoreId { get; private set; }
    public Guid ToStoreId { get; private set; }
    public DateOnly TransferDate { get; private set; }
    public string? Remarks { get; private set; }

    public IReadOnlyList<PharmacyStockTransferLine> Lines => _lines;

    // Required by EF Core materialization.
    private PharmacyStockTransfer()
    {
    }

    private PharmacyStockTransfer(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacyStockTransfer Create(string transferNo, Guid fromStoreId, Guid toStoreId, DateOnly transferDate, string? remarks, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(transferNo, nameof(transferNo));
        if (fromStoreId == toStoreId) throw new ArgumentException("A transfer needs two different stores.", nameof(toStoreId));

        return new PharmacyStockTransfer(Guid.CreateVersion7(), createdBy)
        {
            TransferNo = transferNo,
            FromStoreId = fromStoreId,
            ToStoreId = toStoreId,
            TransferDate = transferDate,
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
        };
    }

    public PharmacyStockTransferLine AddLine(Guid productId, string productName, Guid productBatchId, string batchNo, DateOnly expiryDate, decimal quantity)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));

        var line = new PharmacyStockTransferLine(Id, _lines.Count + 1, productId, productName, productBatchId, batchNo, expiryDate, quantity, CreatedBy);
        _lines.Add(line);
        return line;
    }
}

internal class PharmacyStockTransferLine : Entity
{
    public Guid TransferId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public Guid ProductBatchId { get; private set; }
    public string BatchNo { get; private set; } = null!;
    public DateOnly ExpiryDate { get; private set; }
    public decimal Quantity { get; private set; }

    // Required by EF Core materialization.
    private PharmacyStockTransferLine()
    {
    }

    internal PharmacyStockTransferLine(Guid transferId, int lineNo, Guid productId, string productName, Guid productBatchId, string batchNo, DateOnly expiryDate, decimal quantity, Guid? createdBy)
        : base(Guid.CreateVersion7(), createdBy)
    {
        TransferId = transferId;
        LineNo = lineNo;
        ProductId = productId;
        ProductName = productName;
        ProductBatchId = productBatchId;
        BatchNo = batchNo;
        ExpiryDate = expiryDate;
        Quantity = quantity;
    }
}

/// <summary>
/// A physical count of a store's shelves. Starting one lists every batch the books say the
/// store holds; the counted quantities are entered against it (in as many sittings as it
/// takes); posting it turns every difference into a stock adjustment. Until it is posted it
/// changes nothing.
/// </summary>
internal class PharmacyStockCount : Entity
{
    private readonly List<PharmacyStockCountLine> _lines = [];

    public string CountNo { get; private set; } = null!;
    public Guid StoreId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public StockCountStatus Status { get; private set; }
    public DateTime? PostedAt { get; private set; }

    /// <summary>The adjustment the differences were posted as. Null when the count matched the books.</summary>
    public Guid? AdjustmentId { get; private set; }
    public string? Remarks { get; private set; }

    public IReadOnlyList<PharmacyStockCountLine> Lines => _lines;

    // Required by EF Core materialization.
    private PharmacyStockCount()
    {
    }

    private PharmacyStockCount(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacyStockCount Start(string countNo, Guid storeId, string? remarks, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(countNo, nameof(countNo));

        return new PharmacyStockCount(Guid.CreateVersion7(), createdBy)
        {
            CountNo = countNo,
            StoreId = storeId,
            StartedAt = DateTime.UtcNow,
            Status = StockCountStatus.Open,
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
        };
    }

    public PharmacyStockCountLine AddLine(Guid productId, string productName, Guid productBatchId, string batchNo, DateOnly expiryDate, decimal bookQuantity)
    {
        EnsureOpen();
        var line = new PharmacyStockCountLine(Id, productId, productName, productBatchId, batchNo, expiryDate, bookQuantity, CreatedBy);
        _lines.Add(line);
        return line;
    }

    /// <summary>Enters (or clears, with null) what was counted on one line.</summary>
    public void RecordCount(Guid lineId, decimal? countedQuantity, Guid? updatedBy)
    {
        EnsureOpen();
        if (countedQuantity < 0) throw new ArgumentOutOfRangeException(nameof(countedQuantity), "A counted quantity cannot be negative.");

        var line = _lines.FirstOrDefault(l => l.Id == lineId) ?? throw new InvalidOperationException("That line is not on this count.");
        line.RecordCount(countedQuantity, updatedBy);
        MarkUpdated(updatedBy);
    }

    public void MarkPosted(Guid? adjustmentId, Guid? updatedBy)
    {
        EnsureOpen();
        Status = StockCountStatus.Posted;
        PostedAt = DateTime.UtcNow;
        AdjustmentId = adjustmentId;
        MarkUpdated(updatedBy);
    }

    public void Cancel(Guid? updatedBy)
    {
        EnsureOpen();
        Status = StockCountStatus.Cancelled;
        MarkUpdated(updatedBy);
    }

    private void EnsureOpen()
    {
        if (Status != StockCountStatus.Open)
        {
            throw new InvalidOperationException($"Count {CountNo} is {Status.ToString().ToLowerInvariant()} and can no longer be changed.");
        }
    }
}

internal class PharmacyStockCountLine : Entity
{
    public Guid StockCountId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public Guid ProductBatchId { get; private set; }
    public string BatchNo { get; private set; } = null!;
    public DateOnly ExpiryDate { get; private set; }

    /// <summary>What the books held when the count was started — printed on the count sheet.</summary>
    public decimal BookQuantity { get; private set; }

    /// <summary>What was found on the shelf. Null until this line has been counted.</summary>
    public decimal? CountedQuantity { get; private set; }

    // Required by EF Core materialization.
    private PharmacyStockCountLine()
    {
    }

    internal PharmacyStockCountLine(Guid stockCountId, Guid productId, string productName, Guid productBatchId, string batchNo, DateOnly expiryDate, decimal bookQuantity, Guid? createdBy)
        : base(Guid.CreateVersion7(), createdBy)
    {
        StockCountId = stockCountId;
        ProductId = productId;
        ProductName = productName;
        ProductBatchId = productBatchId;
        BatchNo = batchNo;
        ExpiryDate = expiryDate;
        BookQuantity = bookQuantity;
    }

    internal void RecordCount(decimal? countedQuantity, Guid? updatedBy)
    {
        CountedQuantity = countedQuantity;
        MarkUpdated(updatedBy);
    }
}
