using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public enum DiagnosticTestServiceType
{
    Laboratory = 0,
    Radiology = 1,
    /// <summary>Minor procedures/bedside interventions (e.g. wound dressing, suturing) — a
    /// billable service priced the same way as a lab/radiology test, not literally a
    /// "diagnostic test," but reuses this entity rather than standing up a near-identical
    /// third master for one more service category.</summary>
    Procedure = 2,

    /// <summary>Injections administered during a visit (IM/SC/ID, Direct IV, IV Drip, Blood
    /// Transfusion…) — priced the same way, reuses this entity rather than a new master.</summary>
    Injection = 3,

    /// <summary>File/documentation charges (General Blue File, ANC File, Neonatal File…) —
    /// priced the same way, reuses this entity rather than a new master.</summary>
    File = 4,
}

public record CreateDiagnosticTestRequest
{
    public string Name { get; init; } = string.Empty;
    public DiagnosticTestServiceType ServiceType { get; init; }
    public string? Category { get; init; }
    public decimal Price { get; init; }
    public bool IsOutsourced { get; init; }
    /// <summary>Reference lab the sample is routed to (e.g. "Q-LAB") — only meaningful when <see cref="IsOutsourced"/> is true.</summary>
    public string? ReferenceLab { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateDiagnosticTestRequest
{
    public string Name { get; init; } = string.Empty;
    public DiagnosticTestServiceType ServiceType { get; init; }
    public string? Category { get; init; }
    public decimal Price { get; init; }
    public bool IsOutsourced { get; init; }
    public string? ReferenceLab { get; init; }
    public bool IsActive { get; init; } = true;
}

public record DiagnosticTestResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DiagnosticTestServiceType ServiceType { get; init; }
    public string? Category { get; init; }
    public decimal Price { get; init; }
    public bool IsOutsourced { get; init; }
    public string? ReferenceLab { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class DiagnosticTestListQuery : PagedRequest
{
    public DiagnosticTestServiceType? ServiceType { get; set; }
    public bool? IsOutsourced { get; set; }
    public bool? IsActive { get; set; }
}
