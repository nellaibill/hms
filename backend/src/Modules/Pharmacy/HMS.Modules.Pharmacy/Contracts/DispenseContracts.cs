using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Contracts;

public record CreateDispenseRequest
{
    public Guid ProductId { get; init; }
    public Guid ProductBatchId { get; init; }
    public Guid PatientId { get; init; }
    public Guid? AdmissionId { get; init; }
    public decimal Quantity { get; init; }
    public string? Remarks { get; init; }
}

/// <summary>
/// Denormalizes ProductName/BatchNo/PatientName (resolved once per request in DispenseService
/// via IProductService/IProductBatchService/IPatientService, not stored) — same pattern as
/// IPD's AdmissionResponse.
/// </summary>
public record DispenseResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductBatchId { get; init; }
    public string BatchNo { get; init; } = string.Empty;
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public Guid? AdmissionId { get; init; }
    public decimal Quantity { get; init; }
    public decimal BalanceAfter { get; init; }
    public DateTime TransactionDate { get; init; }
    public string? Remarks { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Set when this dispense was successfully billed — see DispenseService's
    /// best-effort billing step (ADR-028). Null on a Dispense that hasn't been billed, either
    /// because billing hasn't been attempted (never the case for GetById/GetPaged results,
    /// which reflect only what's actually persisted) or because the attempt at creation time
    /// failed — see BillingFailed/BillingError below for that one-time detail.</summary>
    public Guid? InvoiceId { get; init; }

    /// <summary>Populated only on the response returned immediately by CreateAsync (a live
    /// cross-module lookup at the moment of billing success) — always null on GetById/
    /// GetPagedAsync results, to avoid an extra Billing round-trip per row on every list read
    /// for a field the InvoiceId already lets the frontend link to (/finance/accounts/{id}).</summary>
    public string? InvoiceNumber { get; init; }

    /// <summary>True only on the immediate CreateAsync response when the dispense itself
    /// succeeded but the follow-up billing attempt did not — the dispense is NOT rolled back
    /// in that case (see ADR-028's reasoning). Always false on GetById/GetPagedAsync results;
    /// InvoiceId being null there is the persistent signal that a dispense was never billed.</summary>
    public bool BillingFailed { get; init; }

    /// <summary>The billing failure's message, when BillingFailed is true — surfaced to staff
    /// so they know to bill it manually via the OPD Billing Entry screen.</summary>
    public string? BillingError { get; init; }
}

public class DispenseListQuery : PagedRequest
{
    public Guid? PatientId { get; set; }
    public Guid? ProductId { get; set; }
}
