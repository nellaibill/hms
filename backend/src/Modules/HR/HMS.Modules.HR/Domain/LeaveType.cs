using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Domain;

/// <summary>
/// A category of leave an employee can request (e.g. "Casual Leave", "Sick Leave") — a small
/// HR-specific master, kept inside this module (not Masters) since it's not reference data
/// shared by any other module, per docs/DecisionLog.md ADR-036. MaxDaysPerYear null means
/// unlimited; EmployeeService's leave-balance calculation treats that case specially (no
/// meaningful "remaining" figure).
/// </summary>
internal class LeaveType : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int? MaxDaysPerYear { get; private set; }
    public bool IsPaid { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private LeaveType()
    {
    }

    private LeaveType(
        Guid id,
        string code,
        string name,
        int? maxDaysPerYear,
        bool isPaid,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        MaxDaysPerYear = maxDaysPerYear;
        IsPaid = isPaid;
        IsActive = isActive;
    }

    public static LeaveType Create(
        string code,
        string name,
        int? maxDaysPerYear,
        bool isPaid,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new LeaveType(
            Guid.CreateVersion7(),
            code.Trim().ToUpperInvariant(),
            name.Trim(),
            maxDaysPerYear,
            isPaid,
            isActive,
            createdBy);
    }

    public void Update(
        string name,
        int? maxDaysPerYear,
        bool isPaid,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        MaxDaysPerYear = maxDaysPerYear;
        IsPaid = isPaid;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }

    public void Activate(Guid? updatedBy)
    {
        IsActive = true;
        MarkUpdated(updatedBy);
    }

    public void Deactivate(Guid? updatedBy)
    {
        IsActive = false;
        MarkUpdated(updatedBy);
    }
}
