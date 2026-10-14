using HMS.Modules.Pharmacy.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Domain;

/// <summary>
/// An order placed with a supplier. Unlike the documents that move stock it has a life:
/// drafted and edited, then sent, then received against — fully or in parts — as the
/// supplier's invoices are booked. It moves no stock itself; the goods receipt does.
/// Quantities are in packs, as they are ordered.
/// </summary>
internal class PharmacyPurchaseOrder : Entity
{
    private readonly List<PharmacyPurchaseOrderLine> _lines = [];

    public string OrderNo { get; private set; } = null!;
    public Guid StoreId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = null!;
    public DateOnly OrderDate { get; private set; }
    public DateOnly? ExpectedDate { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public string? Remarks { get; private set; }

    public IReadOnlyList<PharmacyPurchaseOrderLine> Lines => _lines;

    /// <summary>What the order is expected to cost before tax, at the rates on its lines.</summary>
    public decimal EstimatedAmount => _lines.Sum(l => l.Quantity * l.ExpectedRate);

    /// <summary>Goods can still be booked against it.</summary>
    public bool IsOpen => Status is PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartlyReceived;

    // Required by EF Core materialization.
    private PharmacyPurchaseOrder()
    {
    }

    private PharmacyPurchaseOrder(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacyPurchaseOrder Create(
        string orderNo, Guid storeId, Guid supplierId, string supplierName, DateOnly orderDate, DateOnly? expectedDate, string? remarks, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(orderNo, nameof(orderNo));
        Guard.AgainstNullOrWhiteSpace(supplierName, nameof(supplierName));

        return new PharmacyPurchaseOrder(Guid.CreateVersion7(), createdBy)
        {
            OrderNo = orderNo,
            StoreId = storeId,
            SupplierId = supplierId,
            SupplierName = supplierName.Trim(),
            OrderDate = orderDate,
            ExpectedDate = expectedDate,
            Status = PurchaseOrderStatus.Draft,
            Remarks = Clean(remarks),
        };
    }

    /// <summary>Changes the header and replaces the lines. Only a draft can be edited: once
    /// sent, the supplier is working from the order as it stood.</summary>
    public void Revise(Guid supplierId, string supplierName, DateOnly? expectedDate, string? remarks, Guid? updatedBy)
    {
        EnsureDraft("edited");
        SupplierId = supplierId;
        SupplierName = supplierName.Trim();
        ExpectedDate = expectedDate;
        Remarks = Clean(remarks);
        _lines.Clear();
        MarkUpdated(updatedBy);
    }

    public PharmacyPurchaseOrderLine AddLine(Guid productId, string productName, decimal quantity, decimal expectedRate, decimal gstRatePercent)
    {
        EnsureDraft("changed");
        Guard.AgainstNonPositive(quantity, nameof(quantity));
        if (expectedRate < 0) throw new ArgumentOutOfRangeException(nameof(expectedRate), "Rate cannot be negative.");

        var line = new PharmacyPurchaseOrderLine(Id, _lines.Count + 1, productId, productName, quantity, expectedRate, gstRatePercent, CreatedBy);
        _lines.Add(line);
        return line;
    }

    public void MarkOrdered(Guid? updatedBy)
    {
        EnsureDraft("sent");
        if (_lines.Count == 0) throw new InvalidOperationException("An order with no lines cannot be sent.");
        Status = PurchaseOrderStatus.Ordered;
        MarkUpdated(updatedBy);
    }

    /// <summary>Withdraws what is still outstanding. Goods already received stay received.</summary>
    public void Cancel(Guid? updatedBy)
    {
        if (Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"Order {OrderNo} is already {Status.ToString().ToLowerInvariant()}.");
        }

        Status = PurchaseOrderStatus.Cancelled;
        MarkUpdated(updatedBy);
    }

    /// <summary>Records packs of a product arriving on a goods receipt. A product that is
    /// not on the order is ignored — suppliers substitute, and the receipt is still valid.</summary>
    public void RecordReceipt(Guid productId, decimal quantity, Guid? updatedBy)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException($"Order {OrderNo} is {Status.ToString().ToLowerInvariant()} and cannot be received against.");
        }

        var line = _lines.FirstOrDefault(l => l.ProductId == productId && l.PendingQuantity > 0) ?? _lines.FirstOrDefault(l => l.ProductId == productId);
        if (line is null || quantity <= 0)
        {
            return;
        }

        line.RecordReceipt(quantity, updatedBy);
        Status = _lines.All(l => l.PendingQuantity <= 0) ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartlyReceived;
        MarkUpdated(updatedBy);
    }

    private void EnsureDraft(string action)
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Order {OrderNo} has been sent and can no longer be {action}.");
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal class PharmacyPurchaseOrderLine : Entity
{
    public Guid PurchaseOrderId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;

    /// <summary>Packs ordered, and packs received so far.</summary>
    public decimal Quantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }

    /// <summary>Per pack, before tax — the rate expected, usually the last one paid.</summary>
    public decimal ExpectedRate { get; private set; }
    public decimal GstRatePercent { get; private set; }

    public decimal PendingQuantity => Math.Max(Quantity - ReceivedQuantity, 0m);

    // Required by EF Core materialization.
    private PharmacyPurchaseOrderLine()
    {
    }

    internal PharmacyPurchaseOrderLine(Guid purchaseOrderId, int lineNo, Guid productId, string productName, decimal quantity, decimal expectedRate, decimal gstRatePercent, Guid? createdBy)
        : base(Guid.CreateVersion7(), createdBy)
    {
        PurchaseOrderId = purchaseOrderId;
        LineNo = lineNo;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        ExpectedRate = expectedRate;
        GstRatePercent = gstRatePercent;
    }

    internal void RecordReceipt(decimal quantity, Guid? updatedBy)
    {
        // Over-supply is recorded as it happened; the line simply shows more received than ordered.
        ReceivedQuantity += quantity;
        MarkUpdated(updatedBy);
    }
}

/// <summary>
/// Stock sent back to a supplier — expired or near-expiry goods, breakage, an over-supply.
/// Posted on save: the stock leaves the store and the supplier owes the pharmacy its value.
/// Quantities are in units, since a return is rarely whole packs.
/// </summary>
internal class PharmacyPurchaseReturn : Entity
{
    private readonly List<PharmacyPurchaseReturnLine> _lines = [];

    public string ReturnNo { get; private set; } = null!;
    public Guid StoreId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = null!;
    public DateOnly ReturnDate { get; private set; }
    public PurchaseReturnReason Reason { get; private set; }

    /// <summary>The supplier's credit note number, once they issue one.</summary>
    public string? CreditNoteNo { get; private set; }
    public decimal TaxableAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal RoundOff { get; private set; }
    public decimal NetAmount { get; private set; }
    public string? Remarks { get; private set; }

    public IReadOnlyList<PharmacyPurchaseReturnLine> Lines => _lines;

    // Required by EF Core materialization.
    private PharmacyPurchaseReturn()
    {
    }

    private PharmacyPurchaseReturn(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacyPurchaseReturn Create(
        string returnNo, Guid storeId, Guid supplierId, string supplierName, DateOnly returnDate, PurchaseReturnReason reason, string? creditNoteNo, string? remarks, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(returnNo, nameof(returnNo));
        Guard.AgainstNullOrWhiteSpace(supplierName, nameof(supplierName));

        return new PharmacyPurchaseReturn(Guid.CreateVersion7(), createdBy)
        {
            ReturnNo = returnNo,
            StoreId = storeId,
            SupplierId = supplierId,
            SupplierName = supplierName.Trim(),
            ReturnDate = returnDate,
            Reason = reason,
            CreditNoteNo = string.IsNullOrWhiteSpace(creditNoteNo) ? null : creditNoteNo.Trim(),
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
        };
    }

    public PharmacyPurchaseReturnLine AddLine(
        Guid productId, string productName, Guid productBatchId, string batchNo, DateOnly expiryDate, decimal quantity, decimal unitRate, decimal gstRatePercent, decimal taxable, decimal tax)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));

        var line = new PharmacyPurchaseReturnLine(Id, _lines.Count + 1, productId, productName, productBatchId, batchNo, expiryDate, quantity, unitRate, gstRatePercent, taxable, tax, CreatedBy);
        _lines.Add(line);
        TaxableAmount += taxable;
        TaxAmount += tax;
        return line;
    }

    public void Close()
    {
        var total = TaxableAmount + TaxAmount;
        NetAmount = Math.Round(total, 0, MidpointRounding.AwayFromZero);
        RoundOff = NetAmount - total;
    }
}

internal class PharmacyPurchaseReturnLine : Entity
{
    public Guid PurchaseReturnId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public Guid ProductBatchId { get; private set; }
    public string BatchNo { get; private set; } = null!;
    public DateOnly ExpiryDate { get; private set; }

    /// <summary>Units returned, and what the supplier credits for each, before tax.</summary>
    public decimal Quantity { get; private set; }
    public decimal UnitRate { get; private set; }
    public decimal GstRatePercent { get; private set; }
    public decimal TaxableAmount { get; private set; }
    public decimal TaxAmount { get; private set; }

    public decimal LineTotal => TaxableAmount + TaxAmount;

    // Required by EF Core materialization.
    private PharmacyPurchaseReturnLine()
    {
    }

    internal PharmacyPurchaseReturnLine(
        Guid purchaseReturnId, int lineNo, Guid productId, string productName, Guid productBatchId, string batchNo, DateOnly expiryDate,
        decimal quantity, decimal unitRate, decimal gstRatePercent, decimal taxable, decimal tax, Guid? createdBy)
        : base(Guid.CreateVersion7(), createdBy)
    {
        PurchaseReturnId = purchaseReturnId;
        LineNo = lineNo;
        ProductId = productId;
        ProductName = productName;
        ProductBatchId = productBatchId;
        BatchNo = batchNo;
        ExpiryDate = expiryDate;
        Quantity = quantity;
        UnitRate = unitRate;
        GstRatePercent = gstRatePercent;
        TaxableAmount = taxable;
        TaxAmount = tax;
    }
}

/// <summary>Money paid to a supplier. Payments are recorded against the supplier's running
/// account, not against one invoice — which is how a distributor's statement reads.</summary>
internal class PharmacySupplierPayment : Entity
{
    public string PaymentNo { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = null!;
    public DateOnly PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public SupplierPaymentMethod Method { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Remarks { get; private set; }

    // Required by EF Core materialization.
    private PharmacySupplierPayment()
    {
    }

    private PharmacySupplierPayment(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacySupplierPayment Create(
        string paymentNo, Guid supplierId, string supplierName, DateOnly paymentDate, decimal amount, SupplierPaymentMethod method, string? referenceNo, string? remarks, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(paymentNo, nameof(paymentNo));
        Guard.AgainstNonPositive(amount, nameof(amount));

        return new PharmacySupplierPayment(Guid.CreateVersion7(), createdBy)
        {
            PaymentNo = paymentNo,
            SupplierId = supplierId,
            SupplierName = supplierName.Trim(),
            PaymentDate = paymentDate,
            Amount = amount,
            Method = method,
            ReferenceNo = string.IsNullOrWhiteSpace(referenceNo) ? null : referenceNo.Trim(),
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
        };
    }
}
