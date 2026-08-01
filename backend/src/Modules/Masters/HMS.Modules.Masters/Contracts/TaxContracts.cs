using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateTaxRequest
{
    public string TaxCode { get; init; } = string.Empty;
    public string TaxName { get; init; } = string.Empty;
    public string? TaxType { get; init; }
    public decimal RatePercent { get; init; }
    public bool IsInclusive { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateTaxRequest
{
    public string TaxName { get; init; } = string.Empty;
    public string? TaxType { get; init; }
    public decimal RatePercent { get; init; }
    public bool IsInclusive { get; init; }
    public bool IsActive { get; init; } = true;
}

public record TaxResponse
{
    public Guid Id { get; init; }
    public string TaxCode { get; init; } = string.Empty;
    public string TaxName { get; init; } = string.Empty;
    public string? TaxType { get; init; }
    public decimal RatePercent { get; init; }
    public bool IsInclusive { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class TaxListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
