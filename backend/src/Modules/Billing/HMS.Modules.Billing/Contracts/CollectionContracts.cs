using PatientsContracts = HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Billing.Contracts;

/// <summary>
/// Query for <c>GET invoices/collections</c> (ADR-125): the payments taken in a period. Both
/// ends are instants, not dates, so the caller decides where a day starts — the Collection
/// Summary sends the hospital's local midnight, which a server running on UTC can't know.
/// </summary>
public record CollectionListQuery
{
    /// <summary>Start of the period, inclusive.</summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>End of the period, exclusive. At most 366 days after <see cref="From"/>.</summary>
    public DateTimeOffset? To { get; init; }
}

/// <summary>
/// One payment as money actually received (ADR-125): when, how much and how, with the bill
/// line it paid for, so the Collection Summary can split it into consultant and hospital
/// amounts the same way OP Report splits a line. Payments on voided bills (a plain void or an
/// approved revert, whose money goes back) are never returned.
/// </summary>
public record CollectionPaymentResponse
{
    public Guid PaymentId { get; init; }

    /// <summary>When the payment was recorded (UTC).</summary>
    public DateTime PaidAt { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; }
    public string? ReferenceNumber { get; init; }
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PatientUhid { get; init; } = string.Empty;

    /// <summary>The visit's encounter type (OP/IP/Emergency/DayCare/Observation); null when the
    /// visit can't be found.</summary>
    public PatientsContracts.VisitType? EncounterType { get; init; }

    /// <summary>The bill line this payment was for.</summary>
    public InvoiceLineItemResponse LineItem { get; init; } = new();
}
