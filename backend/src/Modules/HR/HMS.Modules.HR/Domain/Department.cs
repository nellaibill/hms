using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Domain;

/// <summary>
/// A hospital department (e.g. "ICU", "Cardiology") — the aggregate root that
/// <see cref="WeeklyRoster"/> and <see cref="ShiftAssignment"/> reference by
/// <c>DepartmentId</c>. Until this entity existed, DepartmentId was a bare, unvalidated
/// Guid with no backing record anywhere in the system — an admin had to know and type the
/// raw id by hand. This closes that gap the same way Shift closes it for ShiftId.
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

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — same convention every
        // other aggregate root in this codebase uses.
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
