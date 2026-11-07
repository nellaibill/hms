using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateConsultationTypeRequest
{
    public string Name { get; init; } = string.Empty;
    /// <summary>Optional short label reports show instead of Name (e.g. "Regular") — blank clears it.</summary>
    public string? ShortName { get; init; }
    /// <summary>Standard fee for this consultation category — omitted (null) when there's no
    /// fixed rate (e.g. "Others / On-call," decided per-visit instead).</summary>
    public decimal? Amount { get; init; }
    public bool IsActive { get; init; } = true;
}

// Code is intentionally absent — a natural-key field, protected from change after creation.
public record UpdateConsultationTypeRequest
{
    public string Name { get; init; } = string.Empty;
    public string? ShortName { get; init; }
    public decimal? Amount { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ConsultationTypeResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ShortName { get; init; }
    public decimal? Amount { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ConsultationTypeListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
