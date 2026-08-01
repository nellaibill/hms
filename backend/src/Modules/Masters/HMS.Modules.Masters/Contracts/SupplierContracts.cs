using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateSupplierRequest
{
    public string SupplierCode { get; init; } = string.Empty;
    public string SupplierName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? TaxId { get; init; }
    public string? Country { get; init; }
    public Guid? PaymentTermId { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateSupplierRequest
{
    public string SupplierName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? TaxId { get; init; }
    public string? Country { get; init; }
    public Guid? PaymentTermId { get; init; }
    public bool IsActive { get; init; } = true;
}

public record SupplierResponse
{
    public Guid Id { get; init; }
    public string SupplierCode { get; init; } = string.Empty;
    public string SupplierName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? TaxId { get; init; }
    public string? Country { get; init; }
    public Guid? PaymentTermId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class SupplierListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
