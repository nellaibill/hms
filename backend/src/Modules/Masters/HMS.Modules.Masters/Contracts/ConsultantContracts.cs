using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

/// <summary>In-house (hospital-employed) vs Visiting (external, scheduled hours) — determines
/// the applicable consultation billing category at invoicing time. See Domain/Consultant.cs.</summary>
public enum ConsultantType
{
    InHouse,
    Visiting,
}

public record CreateConsultantRequest
{
    public string Name { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string? Specialization { get; init; }
    public bool IsActive { get; init; } = true;
    /// <summary>Manual sort weighting for consultant pickers — lower shows first, null sorts
    /// last. See Domain/Consultant.cs's own doc comment.</summary>
    public int? Priority { get; init; }
    public ConsultantType? ConsultantType { get; init; }
    /// <summary>Day names (e.g. "Monday") — see Domain/Consultant.cs's own doc comment.</summary>
    public IReadOnlyList<string> AvailableDays { get; init; } = [];
    public TimeOnly? VisitStartTime { get; init; }
    public TimeOnly? VisitEndTime { get; init; }
}

// Code is intentionally absent — a natural-key field, protected from change after creation.
public record UpdateConsultantRequest
{
    public string Name { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string? Specialization { get; init; }
    public bool IsActive { get; init; } = true;
    public int? Priority { get; init; }
    public ConsultantType? ConsultantType { get; init; }
    public IReadOnlyList<string> AvailableDays { get; init; } = [];
    public TimeOnly? VisitStartTime { get; init; }
    public TimeOnly? VisitEndTime { get; init; }
}

public record ConsultantResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string? Specialization { get; init; }
    public bool IsActive { get; init; }
    public int? Priority { get; init; }
    public ConsultantType? ConsultantType { get; init; }
    public IReadOnlyList<string> AvailableDays { get; init; } = [];
    public TimeOnly? VisitStartTime { get; init; }
    public TimeOnly? VisitEndTime { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ConsultantListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
    public Guid? DepartmentId { get; set; }
}
