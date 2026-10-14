using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Contracts;

/// <summary>How money changed hands at the pharmacy counter.</summary>
public enum SalePaymentMethod
{
    Cash,
    Card,
    Upi,
    BankTransfer,
}

// ---- Counter item search ---------------------------------------------------------------

/// <summary>One batch of an item the counter can sell right now, earliest expiry first.</summary>
public record CounterBatchResponse
{
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public decimal QuantityOnHand { get; init; }

    /// <summary>Per unit, tax included: what one tablet of this batch sells for, and its MRP.</summary>
    public decimal UnitPrice { get; init; }
    public decimal UnitMrp { get; init; }
}

/// <summary>
/// Everything the counter needs to put an item on a bill, in one answer: the product, what
/// the store has of it, and its sellable batches with prices — so adding a line is one
/// request, not three.
/// </summary>
public record CounterItemResponse
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public string? Strength { get; init; }
    public DosageForm? DosageForm { get; init; }
    public DrugSchedule Schedule { get; init; }
    public string? RackLocation { get; init; }
    public string? HsnCode { get; init; }
    public decimal UnitsPerPack { get; init; }
    public decimal GstRatePercent { get; init; }

    /// <summary>Sellable units in the store: active, unexpired batches only.</summary>
    public decimal AvailableQuantity { get; init; }

    /// <summary>Price of the batch that will be sold first.</summary>
    public decimal UnitPrice { get; init; }
    public IReadOnlyList<CounterBatchResponse> Batches { get; init; } = [];
}

// ---- Sale ------------------------------------------------------------------------------

public record SaleLineRequest
{
    public Guid ProductId { get; init; }

    /// <summary>The batch to sell from. Omitted, the earliest-expiring stock is used, across
    /// as many batches as the quantity needs.</summary>
    public Guid? ProductBatchId { get; init; }

    /// <summary>Units (tablets, not strips).</summary>
    public decimal Quantity { get; init; }

    /// <summary>This line's own discount. Omitted, the bill's discount applies.</summary>
    public decimal? DiscountPercent { get; init; }
}

public record SalePaymentRequest
{
    public SalePaymentMethod Method { get; init; }

    /// <summary>As tendered. Cash may exceed what is due; the difference is change.</summary>
    public decimal Amount { get; init; }
    public string? ReferenceNo { get; init; }
}

public record CreateSaleRequest
{
    /// <summary>The store selling. Omitted, it is the default store.</summary>
    public Guid? StoreId { get; init; }

    /// <summary>A registered patient. Omitted, this is a walk-in sale and
    /// <see cref="CustomerName"/> is what goes on the bill.</summary>
    public Guid? PatientId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? DoctorName { get; init; }
    public Guid? AdmissionId { get; init; }
    public Guid? PrescriptionId { get; init; }
    public Guid? IndentId { get; init; }

    /// <summary>Discount on every line that does not carry its own.</summary>
    public decimal DiscountPercent { get; init; }
    public IReadOnlyList<SaleLineRequest> Lines { get; init; } = [];
    public IReadOnlyList<SalePaymentRequest> Payments { get; init; } = [];

    /// <summary>Lets the bill be saved without full payment — the rest is owed. Only for a
    /// registered patient; a walk-in pays in full.</summary>
    public bool AllowCredit { get; init; }

    /// <summary>Puts the unpaid part on the patient's admission, to be settled with the
    /// hospital bill. Needs <see cref="AdmissionId"/>.</summary>
    public bool ChargeToAdmission { get; init; }

    /// <summary>The held bill this sale came from; it is removed once the sale is saved.</summary>
    public Guid? HeldBillId { get; init; }
    public string? Remarks { get; init; }
}

public record SaleLineResponse
{
    public Guid Id { get; init; }
    public int LineNo { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public string? HsnCode { get; init; }
    public DrugSchedule Schedule { get; init; }
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal GstRatePercent { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal LineTotal { get; init; }
    public decimal ReturnedQuantity { get; init; }
}

public record SalePaymentResponse
{
    public Guid Id { get; init; }
    public SalePaymentMethod Method { get; init; }
    public decimal Amount { get; init; }
    public string? ReferenceNo { get; init; }
    public DateTime PaidAt { get; init; }
}

public record SaleResponse
{
    public Guid Id { get; init; }
    public string BillNo { get; init; } = string.Empty;
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public DateTime SaleDate { get; init; }
    public Guid? PatientId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? PatientUhid { get; init; }
    public string? CustomerPhone { get; init; }
    public string? DoctorName { get; init; }
    public Guid? AdmissionId { get; init; }
    public Guid? PrescriptionId { get; init; }
    public Guid? IndentId { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal RoundOff { get; init; }
    public decimal NetAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RefundedAmount { get; init; }
    public decimal ReturnedAmount { get; init; }
    public decimal BalanceDue { get; init; }
    public bool ChargedToAdmission { get; init; }

    /// <summary>Cash handed back at the counter when this bill was made. Only on the
    /// response to creating it — it is not stored.</summary>
    public decimal ChangeGiven { get; init; }
    public string? Remarks { get; init; }
    public int LineCount { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Empty on list responses; filled when a single bill is fetched.</summary>
    public IReadOnlyList<SaleLineResponse> Lines { get; init; } = [];
    public IReadOnlyList<SalePaymentResponse> Payments { get; init; } = [];
}

/// <summary>A patient's pharmacy bills with their lines, and the returns made against them —
/// what the patient's own record shows under Billing and on the Timeline.</summary>
public record PatientPharmacyBillsResponse
{
    /// <summary>Newest first.</summary>
    public IReadOnlyList<SaleResponse> Bills { get; init; } = [];

    /// <summary>Newest first.</summary>
    public IReadOnlyList<SaleReturnResponse> Returns { get; init; } = [];
}

public class SaleListQuery : PagedRequest
{
    public Guid? StoreId { get; set; }
    public Guid? PatientId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    /// <summary>Only bills with something still owed.</summary>
    public bool? HasDue { get; set; }
}

public record CollectSalePaymentRequest
{
    public SalePaymentMethod Method { get; init; }
    public decimal Amount { get; init; }
    public string? ReferenceNo { get; init; }
}

// ---- Sale return -----------------------------------------------------------------------

public record SaleReturnLineRequest
{
    public Guid SaleLineId { get; init; }
    public decimal Quantity { get; init; }
}

public record CreateSaleReturnRequest
{
    public IReadOnlyList<SaleReturnLineRequest> Lines { get; init; } = [];
    public SalePaymentMethod RefundMethod { get; init; } = SalePaymentMethod.Cash;

    /// <summary>False when the goods are not fit to sell again: the customer is credited but
    /// nothing returns to stock.</summary>
    public bool Restock { get; init; } = true;
    public string? Reason { get; init; }
}

public record SaleReturnLineResponse
{
    public Guid Id { get; init; }
    public Guid SaleLineId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string BatchNo { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal GstRatePercent { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal LineTotal { get; init; }
}

public record SaleReturnResponse
{
    public Guid Id { get; init; }
    public string ReturnNo { get; init; } = string.Empty;
    public Guid SaleId { get; init; }
    public string BillNo { get; init; } = string.Empty;
    public Guid StoreId { get; init; }
    public DateTime ReturnDate { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ReturnAmount { get; init; }
    public decimal RefundAmount { get; init; }
    public SalePaymentMethod RefundMethod { get; init; }
    public bool Restocked { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyList<SaleReturnLineResponse> Lines { get; init; } = [];
}

public class SaleReturnListQuery : PagedRequest
{
    public Guid? StoreId { get; set; }
    public Guid? SaleId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

// ---- Held bills ------------------------------------------------------------------------

public record HoldBillRequest
{
    public Guid? StoreId { get; init; }

    /// <summary>What the cashier will recognise it by — the customer's name, usually.</summary>
    public string Label { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public decimal Amount { get; init; }

    /// <summary>The counter's cart, serialized by the screen that will read it back.</summary>
    public string Payload { get; init; } = string.Empty;
}

public record HeldBillResponse
{
    public Guid Id { get; init; }
    public Guid StoreId { get; init; }
    public string Label { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public decimal Amount { get; init; }
    public string Payload { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
