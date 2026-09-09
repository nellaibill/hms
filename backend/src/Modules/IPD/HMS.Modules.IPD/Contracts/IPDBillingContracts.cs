namespace HMS.Modules.IPD.Contracts;

/// <summary>Returned by IPDBillingService.GenerateFinalBillAsync — just enough for the
/// frontend to link straight to the real Invoice (/finance/accounts/{invoiceId}) without a
/// second round trip. The full invoice content lives in HMS.Modules.Billing, fetched by that
/// page itself. See ADR-066.</summary>
public record GenerateFinalBillResponse
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}
