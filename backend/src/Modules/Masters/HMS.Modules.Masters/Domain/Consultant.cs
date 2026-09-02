using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A consultant/doctor a patient visit can be attributed to — closes the gap Patients'
/// PatientRegistration.Consultant left as free text (per its own doc comment: "Free-text
/// placeholders until the Staff module exists to back these with a real consultant/
/// department master"). DepartmentId is optional and same-module (validated by direct
/// repository check, not a service call) since Department now lives here too.
/// </summary>
internal class Consultant : Entity
{
    public string Name { get; private set; } = null!;
    public Guid? DepartmentId { get; private set; }
    public string? Specialization { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>Manual sort weighting for consultant pickers (Registration, Billing, and
    /// anywhere else ConsultantSelect is used) — lower shows first, matching a real-world
    /// "who do we want reception to see at the top of the list" priority rather than plain
    /// alphabetical. Null means "no priority set", which sorts after every prioritized
    /// consultant (see ConsultantRepository.ApplySort). Not a uniqueness-constrained rank —
    /// two consultants can share the same priority and just tie-break alphabetically.</summary>
    public int? Priority { get; private set; }

    // Required by EF Core materialization.
    private Consultant()
    {
    }

    private Consultant(
        Guid id,
        string name,
        Guid? departmentId,
        string? specialization,
        bool isActive,
        int? priority,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Name = name;
        DepartmentId = departmentId;
        Specialization = specialization;
        IsActive = isActive;
        Priority = priority;
    }

    public static Consultant Create(
        string name,
        Guid? departmentId,
        string? specialization,
        bool isActive,
        int? priority,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new Consultant(
            Guid.CreateVersion7(),
            name.Trim(),
            departmentId,
            specialization?.Trim(),
            isActive,
            priority,
            createdBy);
    }

    public void Update(
        string name,
        Guid? departmentId,
        string? specialization,
        bool isActive,
        int? priority,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        DepartmentId = departmentId;
        Specialization = specialization?.Trim();
        IsActive = isActive;
        Priority = priority;
        MarkUpdated(updatedBy);
    }
}
