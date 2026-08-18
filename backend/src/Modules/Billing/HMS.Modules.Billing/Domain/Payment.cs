using HMS.Modules.Billing.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Billing.Domain;

/// <summary>
/// One payment transaction recorded against an <see cref="InvoiceLineItem"/> — the real
/// receipt trail the mock store never had (it only flipped a line item's status with no
/// record of when/how/how-much was actually collected). CreatedAt/CreatedBy (from Entity)
/// are the paid-at timestamp and the staff member who recorded it; no separate fields are
/// needed for either.
/// </summary>
internal class Payment : Entity
{
    public Guid InvoiceId { get; private set; }
    public Guid InvoiceLineItemId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }

    // Required by EF Core materialization.
    private Payment()
    {
    }

    private Payment(
        Guid id,
        Guid invoiceId,
        Guid invoiceLineItemId,
        decimal amount,
        PaymentMethod method,
        Guid? createdBy)
        : base(id, createdBy)
    {
        InvoiceId = invoiceId;
        InvoiceLineItemId = invoiceLineItemId;
        Amount = amount;
        Method = method;
    }

    public static Payment Create(Guid invoiceId, Guid invoiceLineItemId, decimal amount, PaymentMethod method, Guid? createdBy)
    {
        return new Payment(Guid.CreateVersion7(), invoiceId, invoiceLineItemId, amount, method, createdBy);
    }
}
