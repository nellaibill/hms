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
}

public class DispenseListQuery : PagedRequest
{
    public Guid? PatientId { get; set; }
    public Guid? ProductId { get; set; }
}
