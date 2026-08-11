using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A hospital department (e.g. "ICU", "Cardiology") — the single source of truth used by
/// both HR (WeeklyRoster/ShiftAssignment's DepartmentId) and Patients (a visit's Department).
/// Originally lived in HMS.Modules.HR; consolidated here so the two modules can't drift
/// into two independently-maintained department lists.
/// </summary>
internal class Department : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private Department()
    {
    }

    private Department(
        Guid id,
        string code,
        string name,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        IsActive = isActive;
    }

    public static Department Create(
        string code,
        string name,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new Department(
            Guid.CreateVersion7(),
            code.Trim().ToUpperInvariant(),
            name.Trim(),
            isActive,
            createdBy);
    }

    public void Update(
        string name,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
