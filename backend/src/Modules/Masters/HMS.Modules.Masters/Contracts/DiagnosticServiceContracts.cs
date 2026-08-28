using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateDiagnosticServiceRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    /// <summary>Only Laboratory or Radiology are accepted here — Procedure stays exclusively
    /// on the old DiagnosticTest (enforced by CreateDiagnosticServiceRequestValidator).</summary>
    public DiagnosticTestServiceType ServiceType { get; init; }
    public bool IsOutsourced { get; init; }
    /// <summary>Required when <see cref="IsOutsourced"/> is true.</summary>
    public Guid? ProviderId { get; init; }
    public decimal Price { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateDiagnosticServiceRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public DiagnosticTestServiceType ServiceType { get; init; }
    public bool IsOutsourced { get; init; }
    public Guid? ProviderId { get; init; }
    public decimal Price { get; init; }
    public bool IsActive { get; init; } = true;
}

public record DiagnosticServiceResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public DiagnosticTestServiceType ServiceType { get; init; }
    public bool IsOutsourced { get; init; }
    public Guid? ProviderId { get; init; }
    public decimal Price { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class DiagnosticServiceListQuery : PagedRequest
{
    public Guid? CategoryId { get; set; }
    public DiagnosticTestServiceType? ServiceType { get; set; }
    public bool? IsOutsourced { get; set; }
    public bool? IsActive { get; set; }
}
