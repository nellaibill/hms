using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Domain;

/// <summary>
/// A pharmacy bill — what the counter prints and the customer takes away. It is the record
/// of the sale in every sense: the stock that left (each line names its batch), what was
/// charged and the GST inside it, and how it was paid. Like a goods receipt it is posted when
/// saved and never edited; goods coming back are a PharmacySaleReturn against it.
/// Quantities are in units (tablets), and prices are per unit including tax, as MRP is.
/// </summary>
internal class PharmacySale : Entity
{
    private readonly List<PharmacySaleLine> _lines = [];
    private readonly List<PharmacySalePayment> _payments = [];

    public string BillNo { get; private set; } = null!;
    public Guid StoreId { get; private set; }
    public DateTime SaleDate { get; private set; }

    /// <summary>Null for a walk-in customer, who is known only by the name on the bill.</summary>
    public Guid? PatientId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string? PatientUhid { get; private set; }
    public string? CustomerPhone { get; private set; }
    public string? DoctorName { get; private set; }
    public Guid? AdmissionId { get; private set; }

    /// <summary>The OPD consultation or IPD indent this bill fulfilled, when it came from one.</summary>
    public Guid? PrescriptionId { get; private set; }
    public Guid? IndentId { get; private set; }

    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxableAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal RoundOff { get; private set; }
    public decimal NetAmount { get; private set; }

    /// <summary>Money taken in, money handed back, and the bill value of goods returned. What
    /// is still owed follows from the three — see <see cref="BalanceDue"/>.</summary>
    public decimal PaidAmount { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public decimal ReturnedAmount { get; private set; }

    /// <summary>The bill's unpaid part has been put on the patient's admission, to be settled
    /// with the hospital bill at discharge rather than at this counter.</summary>
    public bool ChargedToAdmission { get; private set; }
    public string? Remarks { get; private set; }

    public IReadOnlyList<PharmacySaleLine> Lines => _lines;
    public IReadOnlyList<PharmacySalePayment> Payments => _payments;

    public decimal BalanceDue => NetAmount - ReturnedAmount - (PaidAmount - RefundedAmount);

    // Required by EF Core materialization.
    private PharmacySale()
    {
    }

    private PharmacySale(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacySale Create(
        string billNo,
        Guid storeId,
        Guid? patientId,
        string customerName,
        string? patientUhid,
        string? customerPhone,
        string? doctorName,
        Guid? admissionId,
        Guid? prescriptionId,
        Guid? indentId,
        string? remarks,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(billNo, nameof(billNo));
        Guard.AgainstNullOrWhiteSpace(customerName, nameof(customerName));

        return new PharmacySale(Guid.CreateVersion7(), createdBy)
        {
            BillNo = billNo,
            StoreId = storeId,
            SaleDate = DateTime.UtcNow,
            PatientId = patientId,
            CustomerName = customerName.Trim(),
            PatientUhid = Clean(patientUhid),
            CustomerPhone = Clean(customerPhone),
            DoctorName = Clean(doctorName),
            AdmissionId = admissionId,
            PrescriptionId = prescriptionId,
            IndentId = indentId,
            Remarks = Clean(remarks),
        };
    }

    public PharmacySaleLine AddLine(
        Guid productId,
        string productName,
        string? genericName,
        string? hsnCode,
        DrugSchedule schedule,
        Guid productBatchId,
        string batchNo,
        DateOnly expiryDate,
        decimal quantity,
        decimal unitPrice,
        decimal? unitCost,
        decimal discountPercent,
        decimal gstRatePercent,
        decimal gross,
        decimal discount,
        decimal taxable,
        decimal tax)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));

        var line = new PharmacySaleLine(
            Id, _lines.Count + 1, productId, productName, genericName, hsnCode, schedule, productBatchId, batchNo, expiryDate, quantity,
            unitPrice, unitCost, discountPercent, gstRatePercent, gross, discount, taxable, tax, CreatedBy);

        _lines.Add(line);
        GrossAmount += gross;
        DiscountAmount += discount;
        TaxableAmount += taxable;
        TaxAmount += tax;
        return line;
    }

    /// <summary>Call once every line is on: settles the bill total in whole rupees.</summary>
    public void Close()
    {
        var total = TaxableAmount + TaxAmount;
        NetAmount = Math.Round(total, 0, MidpointRounding.AwayFromZero);
        RoundOff = NetAmount - total;
    }

    public PharmacySalePayment AddPayment(SalePaymentMethod method, decimal amount, string? referenceNo, Guid? actorId)
    {
        Guard.AgainstNonPositive(amount, nameof(amount));
        if (amount > BalanceDue)
        {
            throw new InvalidOperationException($"Only {BalanceDue:0.00} is due on bill {BillNo}.");
        }

        var payment = new PharmacySalePayment(Id, method, amount, Clean(referenceNo), actorId);
        _payments.Add(payment);
        PaidAmount += amount;
        MarkUpdated(actorId);
        return payment;
    }

    public void ChargeToAdmission(Guid? actorId)
    {
        if (AdmissionId is null)
        {
            throw new InvalidOperationException("Only a bill for an admitted patient can be charged to the admission.");
        }

        ChargedToAdmission = true;
        MarkUpdated(actorId);
    }

    /// <summary>Records goods coming back: their bill value comes off what is owed first, and
    /// only what is left over is money to hand back. Returns the amount to refund.</summary>
    public decimal ApplyReturn(decimal returnedValue, Guid? actorId)
    {
        Guard.AgainstNonPositive(returnedValue, nameof(returnedValue));

        var dueBefore = Math.Max(BalanceDue, 0m);
        var refund = Math.Max(returnedValue - dueBefore, 0m);

        ReturnedAmount += returnedValue;
        RefundedAmount += refund;
        MarkUpdated(actorId);
        return refund;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal class PharmacySaleLine : Entity
{
    public Guid SaleId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public string? GenericName { get; private set; }
    public string? HsnCode { get; private set; }

    /// <summary>The product's schedule on the day — what puts this line in the H1 or
    /// narcotics register, even if the product is rescheduled later.</summary>
    public DrugSchedule Schedule { get; private set; }
    public Guid ProductBatchId { get; private set; }
    public string BatchNo { get; private set; } = null!;
    public DateOnly ExpiryDate { get; private set; }
    public decimal Quantity { get; private set; }

    /// <summary>Per unit, tax included.</summary>
    public decimal UnitPrice { get; private set; }
    public decimal? UnitCost { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal GstRatePercent { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxableAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal ReturnedQuantity { get; private set; }

    public decimal LineTotal => TaxableAmount + TaxAmount;
    public decimal ReturnableQuantity => Quantity - ReturnedQuantity;

    // Required by EF Core materialization.
    private PharmacySaleLine()
    {
    }

    internal PharmacySaleLine(
        Guid saleId, int lineNo, Guid productId, string productName, string? genericName, string? hsnCode, DrugSchedule schedule,
        Guid productBatchId, string batchNo, DateOnly expiryDate, decimal quantity, decimal unitPrice, decimal? unitCost,
        decimal discountPercent, decimal gstRatePercent, decimal gross, decimal discount, decimal taxable, decimal tax, Guid? createdBy)
        : base(Guid.CreateVersion7(), createdBy)
    {
        SaleId = saleId;
        LineNo = lineNo;
        ProductId = productId;
        ProductName = productName;
        GenericName = genericName;
        HsnCode = hsnCode;
        Schedule = schedule;
        ProductBatchId = productBatchId;
        BatchNo = batchNo;
        ExpiryDate = expiryDate;
        Quantity = quantity;
        UnitPrice = unitPrice;
        UnitCost = unitCost;
        DiscountPercent = discountPercent;
        GstRatePercent = gstRatePercent;
        GrossAmount = gross;
        DiscountAmount = discount;
        TaxableAmount = taxable;
        TaxAmount = tax;
    }

    public void RecordReturn(decimal quantity, Guid? updatedBy)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));
        if (quantity > ReturnableQuantity)
        {
            throw new InvalidOperationException($"Only {ReturnableQuantity:0.####} of {ProductName} is left to return on this bill.");
        }

        ReturnedQuantity += quantity;
        MarkUpdated(updatedBy);
    }
}

internal class PharmacySalePayment : Entity
{
    public Guid SaleId { get; private set; }
    public SalePaymentMethod Method { get; private set; }
    public decimal Amount { get; private set; }
    public string? ReferenceNo { get; private set; }
    public DateTime PaidAt { get; private set; }

    // Required by EF Core materialization.
    private PharmacySalePayment()
    {
    }

    internal PharmacySalePayment(Guid saleId, SalePaymentMethod method, decimal amount, string? referenceNo, Guid? createdBy)
        : base(Guid.CreateVersion7(), createdBy)
    {
        SaleId = saleId;
        Method = method;
        Amount = amount;
        ReferenceNo = referenceNo;
        PaidAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Goods brought back against a bill. Each line points at the bill line it reverses, so a
/// return can never exceed what was sold, and is credited at the price and discount the
/// customer actually paid.
/// </summary>
internal class PharmacySaleReturn : Entity
{
    private readonly List<PharmacySaleReturnLine> _lines = [];

    public string ReturnNo { get; private set; } = null!;
    public Guid SaleId { get; private set; }
    public string BillNo { get; private set; } = null!;
    public Guid StoreId { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public decimal TaxableAmount { get; private set; }
    public decimal TaxAmount { get; private set; }

    /// <summary>Bill value of the goods returned, settled in whole rupees as the bill was.</summary>
    public decimal ReturnAmount { get; private set; }

    /// <summary>Money handed back — less than <see cref="ReturnAmount"/> when part of the
    /// bill was still unpaid.</summary>
    public decimal RefundAmount { get; private set; }
    public SalePaymentMethod RefundMethod { get; private set; }

    /// <summary>False when the goods came back unfit for sale: the bill is credited but
    /// nothing goes back on the shelf.</summary>
    public bool Restocked { get; private set; }
    public string? Reason { get; private set; }

    public IReadOnlyList<PharmacySaleReturnLine> Lines => _lines;

    // Required by EF Core materialization.
    private PharmacySaleReturn()
    {
    }

    private PharmacySaleReturn(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacySaleReturn Create(string returnNo, PharmacySale sale, SalePaymentMethod refundMethod, bool restocked, string? reason, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(returnNo, nameof(returnNo));

        return new PharmacySaleReturn(Guid.CreateVersion7(), createdBy)
        {
            ReturnNo = returnNo,
            SaleId = sale.Id,
            BillNo = sale.BillNo,
            StoreId = sale.StoreId,
            ReturnDate = DateTime.UtcNow,
            CustomerName = sale.CustomerName,
            RefundMethod = refundMethod,
            Restocked = restocked,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
        };
    }

    public PharmacySaleReturnLine AddLine(PharmacySaleLine saleLine, decimal quantity, decimal taxable, decimal tax)
    {
        var line = new PharmacySaleReturnLine(Id, saleLine, quantity, taxable, tax, CreatedBy);
        _lines.Add(line);
        TaxableAmount += taxable;
        TaxAmount += tax;
        return line;
    }

    /// <summary>The lines' value before it is settled in whole rupees.</summary>
    public decimal LinesTotal => TaxableAmount + TaxAmount;

    public void Settle(decimal returnAmount, decimal refundAmount)
    {
        ReturnAmount = returnAmount;
        RefundAmount = refundAmount;
    }
}

internal class PharmacySaleReturnLine : Entity
{
    public Guid SaleReturnId { get; private set; }
    public Guid SaleLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public Guid ProductBatchId { get; private set; }
    public string BatchNo { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal GstRatePercent { get; private set; }
    public decimal TaxableAmount { get; private set; }
    public decimal TaxAmount { get; private set; }

    public decimal LineTotal => TaxableAmount + TaxAmount;

    // Required by EF Core materialization.
    private PharmacySaleReturnLine()
    {
    }

    internal PharmacySaleReturnLine(Guid saleReturnId, PharmacySaleLine saleLine, decimal quantity, decimal taxable, decimal tax, Guid? createdBy)
        : base(Guid.CreateVersion7(), createdBy)
    {
        SaleReturnId = saleReturnId;
        SaleLineId = saleLine.Id;
        ProductId = saleLine.ProductId;
        ProductName = saleLine.ProductName;
        ProductBatchId = saleLine.ProductBatchId;
        BatchNo = saleLine.BatchNo;
        Quantity = quantity;
        UnitPrice = saleLine.UnitPrice;
        GstRatePercent = saleLine.GstRatePercent;
        TaxableAmount = taxable;
        TaxAmount = tax;
    }
}

/// <summary>
/// A bill parked at the counter — the customer went to fetch money, or the next one is in a
/// hurry. Nothing about it is posted: it is the counter's cart saved as it stood, to be
/// recalled, checked against stock as it then is, and billed (or thrown away).
/// </summary>
internal class PharmacyHeldBill : Entity
{
    public Guid StoreId { get; private set; }
    public string Label { get; private set; } = null!;
    public int ItemCount { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>The cart, as the counter screen serialized it. Opaque to the server.</summary>
    public string Payload { get; private set; } = null!;

    // Required by EF Core materialization.
    private PharmacyHeldBill()
    {
    }

    private PharmacyHeldBill(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static PharmacyHeldBill Create(Guid storeId, string label, int itemCount, decimal amount, string payload, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(label, nameof(label));
        Guard.AgainstNullOrWhiteSpace(payload, nameof(payload));

        return new PharmacyHeldBill(Guid.CreateVersion7(), createdBy)
        {
            StoreId = storeId,
            Label = label.Trim(),
            ItemCount = itemCount,
            Amount = amount,
            Payload = payload,
        };
    }
}
