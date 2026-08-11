using HMS.Modules.IPD.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A hospital ward that inpatient beds belong to (e.g. "General Medicine Ward A").
/// DepartmentId references Masters' Department — Masters is the single source of truth
/// for departments, IPD only stores the foreign key (see docs/DecisionLog.md).
/// </summary>
internal class Ward : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid DepartmentId { get; private set; }
    public WardType WardType { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private Ward()
    {
    }

    private Ward(
        Guid id,
        string code,
        string name,
        Guid departmentId,
        WardType wardType,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        DepartmentId = departmentId;
        WardType = wardType;
        IsActive = isActive;
    }

    public static Ward Create(
        string code,
        string name,
        Guid departmentId,
        WardType wardType,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new Ward(
            Guid.CreateVersion7(),
            code.Trim().ToUpperInvariant(),
            name.Trim(),
            departmentId,
            wardType,
            isActive,
            createdBy);
    }

    public void Update(
        string name,
        Guid departmentId,
        WardType wardType,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        DepartmentId = departmentId;
        WardType = wardType;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
